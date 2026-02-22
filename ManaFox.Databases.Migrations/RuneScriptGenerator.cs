using ManaFox.Core.Flow;
using Microsoft.SqlServer.Dac;
using System.Text;
using System.Xml.Linq;

namespace ManaFox.Databases.Migrations;

public class RuneScriptGenerator
{
    private string _sourceDacpacPath = string.Empty;
    private string _targetConnectionString = string.Empty;
    private ScriptGenerationOptions _options = new();

    private RuneScriptGenerator()
    {
    }

    public static Ritual<RuneScriptGenerator> Create()
    {
        return Ritual<RuneScriptGenerator>.Flow(new RuneScriptGenerator());
    }

    public Ritual<RuneScriptGenerator> FromDacpac(string dacpacPath)
    {
        if (string.IsNullOrWhiteSpace(dacpacPath))
            return Ritual<RuneScriptGenerator>.Tear("DACPAC path cannot be empty");

        if (!File.Exists(dacpacPath))
            return Ritual<RuneScriptGenerator>.Tear($"DACPAC file not found: {dacpacPath}");

        _sourceDacpacPath = dacpacPath;
        return Ritual<RuneScriptGenerator>.Flow(this);
    }

    public Ritual<RuneScriptGenerator> FromSqlProject(string sqlProjectPath)
    {
        if (string.IsNullOrWhiteSpace(sqlProjectPath))
            return Ritual<RuneScriptGenerator>.Tear("SQL project path cannot be empty");

        if (!File.Exists(sqlProjectPath))
            return Ritual<RuneScriptGenerator>.Tear($"SQL project file not found: {sqlProjectPath}");

        try
        {
            var projectDir = Path.GetDirectoryName(sqlProjectPath)!;
            var projectName = Path.GetFileNameWithoutExtension(sqlProjectPath);

            // Try common build output locations
            var possibleDacpacPaths = new[]
            {
                Path.Combine(projectDir, "bin", "Debug", $"{projectName}.dacpac"),
                Path.Combine(projectDir, "bin", "Release", $"{projectName}.dacpac"),
                Path.Combine(projectDir, "bin", $"{projectName}.dacpac"),
            };

            var dacpacPath = possibleDacpacPaths.FirstOrDefault(File.Exists);

            if (string.IsNullOrEmpty(dacpacPath))
            {
                // If not found, try parsing the project file for custom output paths
                var projectFile = XDocument.Load(sqlProjectPath);
                var ns = projectFile.Root?.Name.NamespaceName ?? "";
                var outputPath = projectFile.Descendants(XName.Get("OutputPath", ns))
                    .FirstOrDefault()?.Value ?? "bin\\Debug\\";

                var customPath = Path.Combine(projectDir, outputPath.TrimEnd('\\'), $"{projectName}.dacpac");

                if (File.Exists(customPath))
                {
                    dacpacPath = customPath;
                }
                else
                {
                    return Ritual<RuneScriptGenerator>.Tear(
                        $"DACPAC not found. Expected locations: {string.Join(", ", possibleDacpacPaths)}. " +
                        "Please build the SQL project first.");
                }
            }

            _sourceDacpacPath = dacpacPath;
            return Ritual<RuneScriptGenerator>.Flow(this);
        }
        catch (Exception ex)
        {
            return Ritual<RuneScriptGenerator>.Tear($"Error processing SQL project: {ex.Message}");
        }
    }

    public Ritual<RuneScriptGenerator> ToDatabase(string targetConnectionString)
    {
        if (string.IsNullOrWhiteSpace(targetConnectionString))
            return Ritual<RuneScriptGenerator>.Tear("Target connection string cannot be empty");

        _targetConnectionString = targetConnectionString;
        return Ritual<RuneScriptGenerator>.Flow(this);
    }

    public Ritual<RuneScriptGenerator> WithOptions(ScriptGenerationOptions options)
    {
        if (options == null)
            return Ritual<RuneScriptGenerator>.Tear("Script generation options cannot be null");

        _options = options;
        return Ritual<RuneScriptGenerator>.Flow(this);
    }

    public Ritual<ScriptGenerationResult> GenerateTo(string outputFolder)
    {
        return Ritual<ScriptGenerationResult>.Try(() =>
        {
            if (string.IsNullOrWhiteSpace(outputFolder))
                throw new ArgumentException("Output folder cannot be empty", nameof(outputFolder));

            if (string.IsNullOrWhiteSpace(_sourceDacpacPath))
                throw new InvalidOperationException("Source DACPAC must be configured via FromDacpac()");

            if (string.IsNullOrWhiteSpace(_targetConnectionString))
                throw new InvalidOperationException("Target database must be configured via ToDatabase()");

            var startTime = DateTime.UtcNow;
            Directory.CreateDirectory(outputFolder);

            // Load source DACPAC
            using var sourceDac = DacPackage.Load(_sourceDacpacPath);
            // Create DacFx services to handle comparison and script generation
            var dacServices = new DacServices(_targetConnectionString);

            // Configure deployment options
            var deployOptions = new DacDeployOptions
            {
                BlockOnPossibleDataLoss = true,
                DropObjectsNotInSource = _options.IncludeDropStatements,
                GenerateSmartDefaults = true
            };

            // Generate deployment script
            string deploymentScript = dacServices.GenerateDeployScript(sourceDac, _targetConnectionString, deployOptions);

            // Save the generated script
            var fileName = GenerateScriptFileName();
            var filePath = Path.Combine(outputFolder, fileName);
            File.WriteAllText(filePath, deploymentScript, Encoding.UTF8);

            var scriptInfo = new FileInfo(filePath);
            List<string> generatedScripts = string.IsNullOrWhiteSpace(deploymentScript) ? [] : [fileName];

            return new ScriptGenerationResult
            {
                ScriptsGenerated = generatedScripts.Count,
                OutputFolder = outputFolder,
                GeneratedScripts = generatedScripts,
                TotalSize = scriptInfo.Length,
                Duration = DateTime.UtcNow - startTime,
                Summary = GenerateSummary(deploymentScript)
            };
        });
    }

    private string GenerateScriptFileName()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var prefix = _options.ScriptPrefix ?? "0001_";
        return $"{prefix}Migration_{timestamp}.sql";
    }

    private static string GenerateSummary(string deploymentScript)
    {
        if (string.IsNullOrWhiteSpace(deploymentScript))
            return "No schema differences detected.";

        var lineCount = deploymentScript.Split(Environment.NewLine, StringSplitOptions.None).Length;
        return $"Generated migration script with {lineCount} lines of SQL.";
    }
}
