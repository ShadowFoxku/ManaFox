namespace ManaFox.Databases.Migrations;

/// <summary>
/// Result of a database migration operation
/// </summary>
public class MigrationResult
{
    public int ScriptsExecuted { get; init; }
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<string> ExecutedScripts { get; init; } = [];
}