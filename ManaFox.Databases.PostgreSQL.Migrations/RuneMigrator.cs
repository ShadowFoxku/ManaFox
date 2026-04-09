using ManaFox.Core.Flow;
using Npgsql;
using System.Text;

namespace ManaFox.Databases.PostgreSQL.Migrations
{
    public class RuneMigrator
    {
        private string _connectionString = string.Empty;
        private readonly List<string> _sqlFolders = [];
        private bool _createIfNotExists = true;
        private MigratorOptions _options = MigratorOptions.Default;

        private RuneMigrator() { }

        public static Ritual<RuneMigrator> Create()
        {
            return Ritual<RuneMigrator>.Flow(new RuneMigrator());
        }

        public Ritual<RuneMigrator> WithConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return Ritual<RuneMigrator>.Tear("Connection string cannot be empty");

            _connectionString = connectionString;
            return Ritual<RuneMigrator>.Flow(this);
        }

        /// <summary>
        /// Registers a folder of .sql definition files (CREATE TABLE, CREATE FUNCTION, etc.) as a schema source. 
        /// </summary>
        public Ritual<RuneMigrator> WithSqlFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return Ritual<RuneMigrator>.Tear("SQL folder path cannot be empty");

            if (!Directory.Exists(folderPath))
                return Ritual<RuneMigrator>.Tear($"SQL folder not found: {folderPath}");

            var sqlFiles = Directory.GetFiles(folderPath, "*.sql", SearchOption.AllDirectories);
            if (sqlFiles.Length == 0)
                return Ritual<RuneMigrator>.Tear($"No .sql files found in: {folderPath}");

            _sqlFolders.Add(folderPath);
            return Ritual<RuneMigrator>.Flow(this);
        }

        public Ritual<RuneMigrator> WithSqlFolders(IEnumerable<string> folderPaths)
        {
            foreach (var path in folderPaths)
            {
                var result = WithSqlFolder(path);
                if (result.IsTorn)
                    return result;
            }
            return Ritual<RuneMigrator>.Flow(this);
        }

        public Ritual<RuneMigrator> CreateDatabaseIfNotExists(bool create = true)
        {
            _createIfNotExists = create;
            return Ritual<RuneMigrator>.Flow(this);
        }

        public Ritual<RuneMigrator> WithOptions(MigratorOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            return Ritual<RuneMigrator>.Flow(this);
        }

        /// <summary>
        /// Applies all registered SQL definition folders to the target database.
        /// Uses a shadow DB to diff the desired schema against the live DB,
        /// then applies only the delta.
        /// </summary>
        public async Task<Ritual<MigrationResult>> Deploy()
        {
            return await Ritual<MigrationResult>.TryAsync(async () =>
            {
                ValidateConfiguration();

                var startTime = DateTime.UtcNow;
                var databaseName = ExtractDatabaseName(_connectionString);
                var results = new List<SchemaDeploymentResult>();

                await EnsureDatabaseExistsAsync(databaseName);

                foreach (var folder in _sqlFolders)
                {
                    var result = await DeployFolderAsync(folder);
                    results.Add(result);
                }

                return new MigrationResult
                {
                    DatabaseName = databaseName,
                    DatabaseExists = true,
                    DeploymentResults = results,
                    Duration = DateTime.UtcNow - startTime
                };
            });
        }

        /// <summary>
        /// Generates the migration SQL that would be applied, without executing it
        /// </summary>
        public async Task<Ritual<ScriptGenerationResult>> GenerateTo(string outputFolder)
        {
            return await Ritual<ScriptGenerationResult>.TryAsync(async () =>
            {
                ValidateConfiguration();

                if (string.IsNullOrWhiteSpace(outputFolder))
                    throw new ArgumentException("Output folder cannot be empty", nameof(outputFolder));

                var startTime = DateTime.UtcNow;
                var databaseName = ExtractDatabaseName(_connectionString);
                Directory.CreateDirectory(outputFolder);

                var generatedFiles = new List<string>();
                long totalSize = 0;

                foreach (var folder in _sqlFolders)
                {
                    var migrationSql = await GenerateMigrationSqlAsync(folder);

                    if (string.IsNullOrWhiteSpace(migrationSql))
                        continue;

                    var fileName = GenerateScriptFileName(folder);
                    var filePath = Path.Combine(outputFolder, fileName);
                    await File.WriteAllTextAsync(filePath, migrationSql, Encoding.UTF8);

                    var info = new FileInfo(filePath);
                    totalSize += info.Length;
                    generatedFiles.Add(fileName);
                }

                return new ScriptGenerationResult
                {
                    OutputFolder = outputFolder,
                    GeneratedScripts = generatedFiles,
                    ScriptsGenerated = generatedFiles.Count,
                    TotalSize = totalSize,
                    Duration = DateTime.UtcNow - startTime,
                    Summary = generatedFiles.Count == 0
                        ? "No schema differences detected across all folders."
                        : $"Generated {generatedFiles.Count} migration script(s)."
                };
            });
        }

        #region Helpers

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("Connection string must be configured via WithConnectionString()");

            if (_sqlFolders.Count == 0)
                throw new InvalidOperationException("At least one SQL folder must be registered via WithSqlFolder()");
        }

        private async Task<SchemaDeploymentResult> DeployFolderAsync(string folder)
        {
            var start = DateTime.UtcNow;
            var migrationSql = await GenerateMigrationSqlAsync(folder);

            if (!string.IsNullOrWhiteSpace(migrationSql))
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = migrationSql;
                await cmd.ExecuteNonQueryAsync();
            }

            return new SchemaDeploymentResult
            {
                FolderPath = folder,
                Duration = DateTime.UtcNow - start,
                ChangesApplied = !string.IsNullOrWhiteSpace(migrationSql)
            };
        }

        /// <summary>
        /// Core shadow-DB diff logic:
        ///   1. Spin up a temporary PostgreSQL container
        ///   2. Apply the desired .sql files to it
        ///   3. Diff its schema against the target DB via information_schema / pg_catalog
        ///   4. Return the delta as executable SQL
        /// </summary>
        private async Task<string> GenerateMigrationSqlAsync(string folder)
        {
            await using var shadow = await ShadowDatabase.CreateAsync();
            await shadow.ApplySqlFolderAsync(folder);

            await using var targetConn = new NpgsqlConnection(_connectionString);
            await targetConn.OpenAsync();

            var shadowSchema = await shadow.ReadSchemaAsync();
            var targetSchema = await PostgresSchemaReader.ReadAsync(targetConn);

            var differ = new SchemaDiffer(_options);
            return differ.GenerateMigration(shadowSchema, targetSchema);
        }

        private async Task EnsureDatabaseExistsAsync(string databaseName)
        {
            if (!_createIfNotExists)
                return;

            var masterConnString = SwapDatabase(_connectionString, "postgres");
            await using var conn = new NpgsqlConnection(masterConnString);
            await conn.OpenAsync();

            // Check existence first — CREATE DATABASE can't be parameterised in PG
            await using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @db";
            checkCmd.Parameters.AddWithValue("db", databaseName);
            var exists = await checkCmd.ExecuteScalarAsync();

            if (exists is null)
            {
                await using var createCmd = conn.CreateCommand();
                // datname is already validated from the connection string builder
                createCmd.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                await createCmd.ExecuteNonQueryAsync();
            }
        }

        private static string ExtractDatabaseName(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var dbName = builder.Database;

            if (string.IsNullOrWhiteSpace(dbName))
                throw new InvalidOperationException(
                    "Connection string must include a Database name");

            return dbName;
        }

        private static string SwapDatabase(string connectionString, string databaseName)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = databaseName
            };
            return builder.ConnectionString;
        }

        private static string GenerateScriptFileName(string folderPath)
        {
            var folderName = new DirectoryInfo(folderPath).Name;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssffff");
            return $"{timestamp}_{folderName}.sql";
        }

        #endregion Helpers
    }
}
