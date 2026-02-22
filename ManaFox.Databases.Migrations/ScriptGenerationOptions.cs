namespace ManaFox.Databases.Migrations;

public class ScriptGenerationOptions
{
    public bool IncludeDropStatements { get; set; } = false;

    public string? ScriptPrefix { get; set; }
}
