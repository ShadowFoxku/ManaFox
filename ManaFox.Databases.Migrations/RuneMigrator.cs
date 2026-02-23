using ManaFox.Core.Flow;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using System.Text;
using System.Xml.Linq;

namespace ManaFox.Databases.Migrations
{
    public class RuneMigrator
    {
        private string _connectionString = string.Empty;
        private readonly List<string> _dacpacPaths = [];
        private bool _createIfNotExists = true;
        private DacDeployOptions _deployOptions = BuildDefaultOptions();

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

        public Ritual<RuneMigrator> WithSqlProject(string sqlProjectPath)
        {
            var result = ResolveDacpacFromProject(sqlProjectPath);
            return result.Match(
                Ritual<RuneMigrator>.Tear,
                res =>  
                    { 
                        _dacpacPaths.Add(res);
                        return Ritual<RuneMigrator>.Flow(this);
                    }
                );
        }

        public Ritual<RuneMigrator> WithSqlProjects(IEnumerable<string> sqlProjectPaths)
        {
            foreach (var path in sqlProjectPaths)
            {
                var result = WithSqlProject(path);
                if (result.IsTorn)
                    return result;
            }
            return Ritual<RuneMigrator>.Flow(this);
        }

        public Ritual<RuneMigrator> WithDacpac(string dacpacPath)
        {
            if (string.IsNullOrWhiteSpace(dacpacPath))
                return Ritual<RuneMigrator>.Tear("DACPAC path cannot be empty");

            if (!File.Exists(dacpacPath))
                return Ritual<RuneMigrator>.Tear($"DACPAC file not found: {dacpacPath}");

            _dacpacPaths.Add(dacpacPath);
            return Ritual<RuneMigrator>.Flow(this);
        }

        public Ritual<RuneMigrator> CreateDatabaseIfNotExists(bool create = true)
        {
            _createIfNotExists = create;
            return Ritual<RuneMigrator>.Flow(this);
        }

        public Ritual<RuneMigrator> WithDeployOptions(DacDeployOptions options)
        {
            if (options == null)
                return Ritual<RuneMigrator>.Tear("Deploy options cannot be null");

            _deployOptions = options;
            return Ritual<RuneMigrator>.Flow(this);
        }

        /// <summary>
        /// Deploys all configured SQL projects/DACPACs to the target database.
        /// Each DACPAC is applied in the order it was registered.
        /// </summary>
        public Ritual<MigrationResult> Deploy()
        {
            return Ritual<MigrationResult>.Try(() =>
            {
                ValidateConfiguration();

                var startTime = DateTime.UtcNow;
                var databaseName = ExtractDatabaseName(_connectionString);
                var results = new List<DacpacDeploymentResult>();

                EnsureDatabaseExists(databaseName);

                foreach (var dacpacPath in _dacpacPaths)
                {
                    var dacResult = DeployDacpac(dacpacPath, databaseName);
                    results.Add(dacResult);
                }

                return new MigrationResult
                {
                    DatabaseName = databaseName,
                    DatabaseExists = _createIfNotExists && results.Any(),
                    DeploymentResults = results,
                    Duration = DateTime.UtcNow - startTime
                };
            });
        }

        /// <summary>
        /// Generates deployment scripts for all configured DACPACs without applying them.
        /// Useful for review or auditing before committing a migration.
        /// </summary>
        public Ritual<ScriptGenerationResult> GenerateTo(string outputFolder)
        {
            return Ritual<ScriptGenerationResult>.Try(() =>
            {
                ValidateConfiguration();

                if (string.IsNullOrWhiteSpace(outputFolder))
                    throw new ArgumentException("Output folder cannot be empty", nameof(outputFolder));

                var startTime = DateTime.UtcNow;
                var databaseName = ExtractDatabaseName(_connectionString);
                Directory.CreateDirectory(outputFolder);

                var generatedFiles = new List<string>();
                long totalSize = 0;

                foreach (var dacpacPath in _dacpacPaths)
                {
                    var dacServices = new DacServices(_connectionString);
                    using var package = DacPackage.Load(dacpacPath);

                    var script = dacServices.GenerateDeployScript(package, databaseName, _deployOptions);

                    if (string.IsNullOrWhiteSpace(script))
                        continue;

                    var fileName = GenerateScriptFileName(dacpacPath);
                    var filePath = Path.Combine(outputFolder, fileName);
                    File.WriteAllText(filePath, script, Encoding.UTF8);

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
                        ? "No schema differences detected across all DACPACs."
                        : $"Generated {generatedFiles.Count} migration script(s)."
                };
            });
        }

        #region Helpers
        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("Connection string must be configured via WithConnectionString()");

            if (_dacpacPaths.Count == 0)
                throw new InvalidOperationException("At least one SQL project or DACPAC must be registered");
        }

        private DacpacDeploymentResult DeployDacpac(string dacpacPath, string databaseName)
        {
            var start = DateTime.UtcNow;
            var dacServices = new DacServices(_connectionString);
            using var package = DacPackage.Load(dacpacPath);

            dacServices.Deploy(package, databaseName, upgradeExisting: true, options: _deployOptions);

            return new DacpacDeploymentResult
            {
                DacpacPath = dacpacPath,
                Duration = DateTime.UtcNow - start
            };
        }

        private void EnsureDatabaseExists(string databaseName)
        {
            if (!_createIfNotExists)
                return;

            // Connect to master to check/create the target database
            var masterConnectionString = SwapDatabase(_connectionString, "master");
            using var connection = new SqlConnection(masterConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""
            IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = @dbName)
            BEGIN
                CREATE DATABASE [{databaseName}]
            END
            """;
            cmd.Parameters.AddWithValue("@dbName", databaseName);
            cmd.ExecuteNonQuery();
        }

        private static string ExtractDatabaseName(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var dbName = builder.InitialCatalog;

            if (string.IsNullOrWhiteSpace(dbName))
                throw new InvalidOperationException(
                    "Connection string must include an Initial Catalog (database name)");

            return dbName;
        }

        private static string SwapDatabase(string connectionString, string databaseName)
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = databaseName
            };
            return builder.ConnectionString;
        }

        private static DacDeployOptions BuildDefaultOptions()
        {
            return new DacDeployOptions
            {
                // Only touch objects the DACPAC knows about
                DropObjectsNotInSource = false,
                // Don't wipe schemas we don't own
                DoNotDropObjectTypes =
                [
                    ObjectType.Users,
                    ObjectType.RoleMembership,
                    ObjectType.Logins
                ],
                BlockOnPossibleDataLoss = true,
                GenerateSmartDefaults = true,
                // Ensure we don't accidentally step on unrelated schemas
                ExcludeObjectTypes = []
            };
        }

        private static string GenerateScriptFileName(string dacpacPath)
        {
            var projectName = Path.GetFileNameWithoutExtension(dacpacPath);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssffff");
            return $"{timestamp}_{projectName}.sql";
        }

        private static Ritual<string> ResolveDacpacFromProject(string sqlProjectPath)
        {
            if (string.IsNullOrWhiteSpace(sqlProjectPath))
                return Ritual<string>.Tear("SQL project path cannot be empty");

            if (!File.Exists(sqlProjectPath))
                return Ritual<string>.Tear($"SQL project file not found: {sqlProjectPath}");

            try
            {
                var projectDir = Path.GetDirectoryName(sqlProjectPath)!;
                var projectName = Path.GetFileNameWithoutExtension(sqlProjectPath);

                var candidates = new[]
                {
                    Path.Combine(projectDir, "bin", "Debug", $"{projectName}.dacpac"),
                    Path.Combine(projectDir, "bin", "Release", $"{projectName}.dacpac"),
                    Path.Combine(projectDir, "bin", $"{projectName}.dacpac"),
                };

                var found = candidates.FirstOrDefault(File.Exists);
                if (found != null)
                    return Ritual<string>.Flow(found);

                // Fall back to parsing the project file for a custom output path
                var projectFile = XDocument.Load(sqlProjectPath);
                var ns = projectFile.Root?.Name.NamespaceName ?? "";
                var outputPath = projectFile.Descendants(XName.Get("OutputPath", ns))
                    .FirstOrDefault()?.Value ?? "bin\\Debug\\";

                var customPath = Path.Combine(projectDir, outputPath.TrimEnd('\\'), $"{projectName}.dacpac");
                if (File.Exists(customPath))
                    return Ritual<string>.Flow(customPath);

                return Ritual<string>.Tear(
                    $"DACPAC not found for '{projectName}'. " +
                    $"Checked: {string.Join(", ", candidates)}. Build the SQL project first.");
            }
            catch (Exception ex)
            {
                return Ritual<string>.Tear($"Error resolving DACPAC from SQL project: {ex.Message}");
            }
        }

        #endregion Helpers
    }
}
