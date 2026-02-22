namespace ManaFox.Databases.Migrations;

public class ScriptGenerationResult
{
    public int ScriptsGenerated { get; init; }

    public string OutputFolder { get; init; } = string.Empty;

    public IReadOnlyList<string> GeneratedScripts { get; init; } = [];

    public long TotalSize { get; init; }

    public TimeSpan Duration { get; init; }

    public string Summary { get; init; } = string.Empty;
}
