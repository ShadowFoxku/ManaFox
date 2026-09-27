using ManaFox.Core.Flow;
using Npgsql;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ManaFox.Databases.PostgreSQL.Migrations
{
    public class RuneMigrator
    {
        private string _connectionString = string.Empty;
        private readonly List<string> _sqlFolders = [];
        private bool _createIfNotExists = true;
        private MigratorOptions _options = MigratorOptions.Default;
        private readonly List<string> _functionFolders = [];

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

            var sqlFiles = GetSqlFiles(folderPath);
            if (sqlFiles.Count == 0)
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

        /// <summary>
        /// Registers a folder of .sql files (CREATE OR REPLACE FUNCTION, CREATE OR REPLACE VIEW, etc.)
        /// that are applied without doing diffs, since I am too lazy to really work that out.
        /// </summary>
        public Ritual<RuneMigrator> WithFunctionsFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return Ritual<RuneMigrator>.Tear("Functions folder path cannot be empty");

            if (!Directory.Exists(folderPath))
                return Ritual<RuneMigrator>.Tear($"Functions folder not found: {folderPath}");

            var sqlFiles = GetSqlFiles(folderPath);
            if (sqlFiles.Count == 0)
                return Ritual<RuneMigrator>.Tear($"No .sql files found in: {folderPath}");

            _functionFolders.Add(folderPath);
            return Ritual<RuneMigrator>.Flow(this);
        }

        public Ritual<RuneMigrator> WithFunctionsFolders(IEnumerable<string> folderPaths)
        {
            foreach (var path in folderPaths)
            {
                var result = WithFunctionsFolder(path);
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

        public Ritual<RuneMigrator> WithLogger(ILogger logger)
        {
            _options = _options with { Logger = logger };
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
        /// then applies only the delta. Function/view folders are applied directly.
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

                if (_sqlFolders.Count > 0)
                {
                    results.Add(await RunStepAsync(
                        "Tables",
                        BuildTablesMigrationSqlAsync,
                        sql => ExecuteSqlAsync(sql, useTransaction: false)));
                }

                if (_functionFolders.Count > 0)
                {
                    results.Add(await RunStepAsync(
                        "Functions",
                        BuildFunctionsSqlAsync,
                        sql => ExecuteSqlAsync(sql, useTransaction: true)));
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
                Directory.CreateDirectory(outputFolder);

                var generatedFiles = new List<string>();
                long totalSize = 0;

                if (_sqlFolders.Count > 0)
                {
                    var sql = await BuildTablesMigrationSqlAsync();
                    if (!string.IsNullOrWhiteSpace(sql))
                        totalSize += await WriteScriptAsync(outputFolder, "tables", sql, generatedFiles);
                }

                if (_functionFolders.Count > 0)
                {
                    var sql = await BuildFunctionsSqlAsync();
                    if (!string.IsNullOrWhiteSpace(sql))
                        totalSize += await WriteScriptAsync(outputFolder, "functions", sql, generatedFiles);
                }

                return new ScriptGenerationResult
                {
                    OutputFolder = outputFolder,
                    GeneratedScripts = generatedFiles,
                    ScriptsGenerated = generatedFiles.Count,
                    TotalSize = totalSize,
                    Duration = DateTime.UtcNow - startTime,
                    Summary = generatedFiles.Count == 0
                        ? "No schema differences detected."
                        : $"Generated {generatedFiles.Count} migration script(s)."
                };
            });
        }

        #region Build
        private async Task<string> BuildTablesMigrationSqlAsync()
        {
            await using var shadow = await ShadowDatabase.CreateAsync();
            await shadow.ApplySqlFoldersAsync(_sqlFolders);

            await using var targetConn = new NpgsqlConnection(_connectionString);
            await targetConn.OpenAsync();

            var shadowSchema = await shadow.ReadSchemaAsync(_options);
            var targetSchema = await PostgresSchemaReader.ReadAsync(targetConn, _options);

            var differ = new SchemaDiffer(_options);
            return differ.GenerateMigration(shadowSchema, targetSchema);
        }
        
        private async Task<string> BuildFunctionsSqlAsync()
        {
            var sqlFiles = _functionFolders.SelectMany(f => GetSqlFiles(f, true)).ToList();

            var sb = new StringBuilder();
            foreach (var file in sqlFiles)
            {
                var sql = await File.ReadAllTextAsync(file);
                if (string.IsNullOrWhiteSpace(sql)) continue;
                sb.AppendLine(sql.Trim());
                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }

        #endregion

        #region Apply

        private static async Task<SchemaDeploymentResult> RunStepAsync(
            string label,
            Func<Task<string>> buildSql,
            Func<string, Task> applySql)
        {
            var start = DateTime.UtcNow;
            var sql = await buildSql();

            if (!string.IsNullOrWhiteSpace(sql))
                await applySql(sql);

            return new SchemaDeploymentResult
            {
                Label = label,
                Duration = DateTime.UtcNow - start,
                ChangesApplied = !string.IsNullOrWhiteSpace(sql)
            };
        }

        private async Task ExecuteSqlAsync(string sql, bool useTransaction)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var statements = SqlBatchExecutor.SplitStatements(sql)
                .Select(s => ("generated diff", s))
                .ToList();

            if (useTransaction)
            {
                await using var transaction = await conn.BeginTransactionAsync();
                await SqlBatchExecutor.ExecuteWithDependencyRetryAsync(conn, statements, transaction);
                await transaction.CommitAsync();
            }
            else
            {
                await SqlBatchExecutor.ExecuteWithDependencyRetryAsync(conn, statements);
            }
        }

        private static async Task<long> WriteScriptAsync(
            string outputFolder, string label, string sql, List<string> generatedFiles)
        {
            var fileName = GenerateScriptFileName(label);
            var filePath = Path.Combine(outputFolder, fileName);
            await File.WriteAllTextAsync(filePath, sql, Encoding.UTF8);

            generatedFiles.Add(fileName);
            return new FileInfo(filePath).Length;
        }

        #endregion

        #region Helpers

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("Connection string must be configured via WithConnectionString()");

            if (_sqlFolders.Count == 0 && _functionFolders.Count == 0)
                throw new InvalidOperationException("At least one SQL folder must be registered via WithSqlFolder() or WithFunctionsFolder()");
        }

        private async Task EnsureDatabaseExistsAsync(string databaseName)
        {
            if (!_createIfNotExists)
                return;

            var masterConnString = SwapDatabase(_connectionString, "postgres");
            await using var conn = new NpgsqlConnection(masterConnString);
            await conn.OpenAsync();

            // Check existence first
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

        private static string GenerateScriptFileName(string label)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssffff");
            return $"{timestamp}_{label}.sql";
        }

        private static List<string> GetSqlFiles(string folder, bool ordered = false)
        {
            var sqlFiles = Directory.GetFiles(folder, "*.sql", SearchOption.AllDirectories);

            if (!ordered)
                return [.. sqlFiles];

            return [.. sqlFiles.OrderBy(f => f)];
        }

        #endregion Helpers
    }
}