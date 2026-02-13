using DbUp;
using ManaFox.Core.Flow;
using System.Reflection;

namespace ManaFox.Databases.Migrations;

/// <summary>
/// DBUp-based database migrator that follows folder conventions
/// </summary>
public class RuneMigrator
{
    private readonly string _connectionString;
    private Assembly? _scriptsAssembly;
    private List<string> _folderPatterns = [];
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
    /// Use the default folder pattern: Tables/{schema}, StoredProcedures, DataTypes
    /// </summary>
    public Ritual<RuneMigrator> UseDefaultFolderPattern()
    {
        _folderPatterns =
        [
            "Tables",
            "StoredProcedures",
            "DataTypes"
        ];
        return Ritual<RuneMigrator>.Flow(this);
    }

    /// <summary>
    /// Specify custom folder patterns for script discovery
    /// </summary>
    public Ritual<RuneMigrator> WithFolderPatterns(params string[] patterns)
    {
        if (patterns == null || patterns.Length == 0)
            return Ritual<RuneMigrator>.Tear("At least one folder pattern is required");

        _folderPatterns = [.. patterns];
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
    /// Execute the database migration
    /// </summary>
    public Ritual<MigrationResult> Run()
    {
        return Ritual<MigrationResult>.Try(() =>
        {
            if (_scriptsAssembly == null)
                throw new InvalidOperationException("Scripts assembly must be configured before running migration");

            if (_folderPatterns.Count == 0)
                throw new InvalidOperationException("At least one folder pattern must be configured");

            var startTime = DateTime.UtcNow;

            // Build DBUp upgrader
            var upgrader = DeployChanges.To
                .SqlDatabase(_connectionString)
                .WithScriptsEmbeddedInAssembly(
                    _scriptsAssembly,
                    script => ScriptMatchesFolderPattern(script, _folderPatterns))
                .LogToConsole();

            if (!string.IsNullOrWhiteSpace(_schemaVersionTable))
            {
                upgrader = upgrader.JournalToSqlTable(_schemaVersionSchema, _schemaVersionTable);
            }

            var result = upgrader.Build().PerformUpgrade();

            if (!result.Successful)
                throw new MigrationException("Database migration failed", result.Error);

            return new MigrationResult
            {
                ScriptsExecuted = result.Scripts.Count(),
                Duration = DateTime.UtcNow - startTime,
                ExecutedScripts = [.. result.Scripts.Select(s => s.Name)]
            };
        });
    }

    private static bool ScriptMatchesFolderPattern(string scriptName, List<string> patterns)
    {
        return patterns.Any(pattern =>
            scriptName.Contains($".{pattern}.", StringComparison.OrdinalIgnoreCase));
    }
}