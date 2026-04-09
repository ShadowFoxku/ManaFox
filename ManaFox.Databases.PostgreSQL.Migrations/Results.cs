using ManaFox.Core.ConsoleTools;
using System.Text;

namespace ManaFox.Databases.PostgreSQL.Migrations
{
    /// <summary>
    /// Result for a single SQL folder deployment.
    /// </summary>
    public record SchemaDeploymentResult
    {
        public required string FolderPath { get; init; }
        public TimeSpan Duration { get; init; }
        public bool ChangesApplied { get; init; }
        public string ProjectName => new DirectoryInfo(FolderPath).Name;
    }

    /// <summary>
    /// Aggregate result for a full migration run across all registered folders.
    /// </summary>
    public record MigrationResult
    {
        public required string DatabaseName { get; init; }
        public bool DatabaseExists { get; init; }
        public required IReadOnlyList<SchemaDeploymentResult> DeploymentResults { get; init; }
        public TimeSpan Duration { get; init; }
        public int FoldersDeployed => DeploymentResults.Count;

        #region Console table printing

        public string GetDeploymentResultsTable(double goodLimit = 10, double dangerLimit = 20)
        {
            int nameWidth = DeploymentResults.Max(r => r.ProjectName.Length);

            var sb = new StringBuilder();
            sb.AppendLine($"{ConsoleConstants.Cyan}┌{new string('─', nameWidth + 2)}┬{new string('─', 10)}┬{new string('─', 19)}┐{ConsoleConstants.Reset}");
            sb.AppendLine($"{ConsoleConstants.Cyan}│{ConsoleConstants.Reset} {"Project".PadRight(nameWidth)} {ConsoleConstants.Cyan}│{ConsoleConstants.Reset} {"Changes".PadRight(8)} {ConsoleConstants.Cyan}│{ConsoleConstants.Reset} {"Duration".PadRight(17)} {ConsoleConstants.Cyan}│{ConsoleConstants.Reset}");
            sb.AppendLine($"{ConsoleConstants.Cyan}├{new string('─', nameWidth + 2)}┼{new string('─', 10)}┼{new string('─', 19)}┤{ConsoleConstants.Reset}");

            foreach (var res in DeploymentResults)
            {
                var colour = GetColour(res.Duration, goodLimit, dangerLimit);
                var changes = res.ChangesApplied ? "Yes" : "None";
                sb.AppendLine(
                    $"{ConsoleConstants.Cyan}│{ConsoleConstants.Reset} {colour}{res.ProjectName.PadRight(nameWidth)}{ConsoleConstants.Reset} " +
                    $"{ConsoleConstants.Cyan}│{ConsoleConstants.Reset} {colour}{changes.PadRight(8)}{ConsoleConstants.Reset} " +
                    $"{ConsoleConstants.Cyan}│{ConsoleConstants.Reset} {colour}{res.Duration.ToString().PadRight(17)}{ConsoleConstants.Reset} " +
                    $"{ConsoleConstants.Cyan}│{ConsoleConstants.Reset}");
            }

            sb.AppendLine($"{ConsoleConstants.Cyan}└{new string('─', nameWidth + 2)}┴{new string('─', 10)}┴{new string('─', 19)}┘{ConsoleConstants.Reset}");

            return sb.ToString();
        }

        private static string GetColour(TimeSpan duration, double goodLimit, double dangerLimit)
        {
            var secs = duration.TotalSeconds;
            if (secs < goodLimit) return ConsoleConstants.Green;
            if (secs < dangerLimit) return ConsoleConstants.Yellow;
            return ConsoleConstants.Red;
        }

        #endregion
    }

    /// <summary>
    /// Result of a GenerateTo() call — scripts written to disk without being applied.
    /// </summary>
    public class ScriptGenerationResult
    {
        public int ScriptsGenerated { get; init; }
        public string OutputFolder { get; init; } = string.Empty;
        public IReadOnlyList<string> GeneratedScripts { get; init; } = [];
        public long TotalSize { get; init; }
        public TimeSpan Duration { get; init; }
        public string Summary { get; init; } = string.Empty;
    }
}
