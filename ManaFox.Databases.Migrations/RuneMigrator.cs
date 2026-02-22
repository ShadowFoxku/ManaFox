using DbUp;
using DbUp.Engine;
using ManaFox.Core.Flow;
using System.Reflection;

namespace ManaFox.Databases.Migrations;

public class RuneMigrator
{
    private readonly string _connectionString;
    private Assembly? _scriptsAssembly;
    private string _migrationsFolder = "Migrations";
    private string? _predeploymentFolder;
    private string? _postdeploymentFolder;
    private string _schemaVersionTable = "SchemaVersions";
    private string _schemaVersionSchema = "dbo";

    private RuneMigrator(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Create a new migrator with the given connection string
    /// </summary>
    public static Ritual<RuneMigrator> Create(string connectionString)
    {
        return Ritual<RuneMigrator>.Try(() =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be empty", nameof(connectionString));

            return new RuneMigrator(connectionString);
        });
    }

    /// <summary>
    /// Specify the assembly containing embedded migration scripts
    /// </summary>
    public Ritual<RuneMigrator> WithEmbeddedScripts(Assembly assembly)
    {
        if (assembly == null)
            return Ritual<RuneMigrator>.Tear("Scripts assembly cannot be null");

        _scriptsAssembly = assembly;
        return Ritual<RuneMigrator>.Flow(this);
    }

    /// <summary>
    /// Set the migrations folder name (default: Migrations)
    /// </summary>
    public Ritual<RuneMigrator> WithMigrationsFolder(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return Ritual<RuneMigrator>.Tear("Migrations folder name cannot be empty");

        _migrationsFolder = folderName;
        return Ritual<RuneMigrator>.Flow(this);
    }

    /// <summary>
    /// Set the pre-deployment scripts folder (optional)
    /// </summary>
    public Ritual<RuneMigrator> WithPredeploymentFolder(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return Ritual<RuneMigrator>.Tear("Pre-deployment folder name cannot be empty");

        _predeploymentFolder = folderName;
        return Ritual<RuneMigrator>.Flow(this);
    }

    /// <summary>
    /// Set the post-deployment scripts folder (optional)
    /// </summary>
    public Ritual<RuneMigrator> WithPostdeploymentFolder(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return Ritual<RuneMigrator>.Tear("Post-deployment folder name cannot be empty");

        _postdeploymentFolder = folderName;
        return Ritual<RuneMigrator>.Flow(this);
    }

    /// <summary>
    /// Specify the schema version table name (default: dbo.SchemaVersions)
    /// </summary>
    public Ritual<RuneMigrator> WithSchemaVersionTable(string tableName, string? schema = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return Ritual<RuneMigrator>.Tear("Schema version table name cannot be empty");

        if (schema != null && string.IsNullOrWhiteSpace(schema))
            return Ritual<RuneMigrator>.Tear("Schema type can not be empty");
        else if (schema != null)
            _schemaVersionSchema = schema;

        _schemaVersionTable = tableName;
        
        return Ritual<RuneMigrator>.Flow(this);
    }

    /// <summary>
    /// Execute the database migration (pre-deployment → migrations → post-deployment)
    /// </summary>
    public Ritual<MigrationResult> Run()
    {
        return Ritual<MigrationResult>.Try(() =>
        {
            if (_scriptsAssembly == null)
                throw new InvalidOperationException("Scripts assembly must be configured via WithEmbeddedScripts()");

            var startTime = DateTime.UtcNow;
            var executedScripts = new List<string>();

            // Run pre-deployment scripts if configured
            if (!string.IsNullOrWhiteSpace(_predeploymentFolder))
            {
                var preResult = RunScriptsInFolder(_predeploymentFolder);
                var scripts = (IEnumerable<DbUp.Engine.SqlScript>)preResult.Scripts;
                executedScripts.AddRange(scripts.Select(s => s.Name));
            }

            // Run main migration scripts
            var mainResult = RunScriptsInFolder(_migrationsFolder);
            var mainScripts = (IEnumerable<DbUp.Engine.SqlScript>)mainResult.Scripts;
            executedScripts.AddRange(mainScripts.Select(s => s.Name));

            // Run post-deployment scripts if configured
            if (!string.IsNullOrWhiteSpace(_postdeploymentFolder))
            {
                var postResult = RunScriptsInFolder(_postdeploymentFolder);
                var postScripts = (IEnumerable<DbUp.Engine.SqlScript>)postResult.Scripts;
                executedScripts.AddRange(postScripts.Select(s => s.Name));
            }

            return new MigrationResult
            {
                ScriptsExecuted = executedScripts.Count,
                Duration = DateTime.UtcNow - startTime,
                ExecutedScripts = executedScripts
            };
        });
    }

    private DatabaseUpgradeResult RunScriptsInFolder(string folderName)
    {
        var upgrader = DeployChanges.To
            .SqlDatabase(_connectionString)
            .WithScriptsEmbeddedInAssembly(
                _scriptsAssembly!,
                script => ScriptIsInFolder(script, folderName))
            .LogToConsole();

        if (!string.IsNullOrWhiteSpace(_schemaVersionTable))
        {
            upgrader = upgrader.JournalToSqlTable(_schemaVersionSchema, _schemaVersionTable);
        }

        var result = upgrader.Build().PerformUpgrade();

        if (!result.Successful)
            throw new MigrationException($"Database migration failed in folder '{folderName}'", result.Error);

        return result;
    }

    private static bool ScriptIsInFolder(string scriptName, string folderName)
    {
        return scriptName.Contains($".{folderName}.", StringComparison.OrdinalIgnoreCase);
    }
}