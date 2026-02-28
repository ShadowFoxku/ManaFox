using ManaFox.Core.ConsoleTools;
using System.Text;

namespace ManaFox.Databases.Migrations
{
    public record MigrationResult
    {
        public required string DatabaseName { get; init; }
        public bool DatabaseExists { get; init; }
        public required IReadOnlyList<DacpacDeploymentResult> DeploymentResults { get; init; }
        public TimeSpan Duration { get; init; }
        public int DacpacsDeployed => DeploymentResults.Count;

        #region Console table printing

        public string GetDeploymentResultsTable(double goodLimit = 10, double dangerLimit = 20)
        {
            int nameWidth = DeploymentResults.Max(r => r.ProjectName.Length);

            StringBuilder sb = new();
            sb.AppendLine($"{ConsoleConstants.Cyan}┌{new string('─', nameWidth + 2)}┬{new string('─', 19)}┐{ConsoleConstants.Reset}");
            sb.AppendLine($"{ConsoleConstants.Cyan}│{ConsoleConstants.Reset} {"Project".PadRight(nameWidth)} {ConsoleConstants.Cyan}│{ConsoleConstants.Reset} {"Duration".PadRight(17)} {ConsoleConstants.Cyan}│{ConsoleConstants.Reset}");
            sb.AppendLine($"{ConsoleConstants.Cyan}├{new string('─', nameWidth + 2)}┼{new string('─', 19)}┤{ConsoleConstants.Reset}");
            foreach (var res in DeploymentResults)
                sb.AppendLine($"{ConsoleConstants.Cyan}│{ConsoleConstants.Reset} {GetColour(res.Duration, goodLimit, dangerLimit)}{res.ProjectName.PadRight(nameWidth)}{ConsoleConstants.Reset} {ConsoleConstants.Cyan}" +
                    $"│{ConsoleConstants.Reset} {GetColour(res.Duration, goodLimit, dangerLimit)}{res.Duration.ToString().PadRight(17)}{ConsoleConstants.Reset} {ConsoleConstants.Cyan}│{ConsoleConstants.Reset}");
            sb.AppendLine($"{ConsoleConstants.Cyan}└{new string('─', nameWidth + 2)}┴{new string('─', 19)}┘{ConsoleConstants.Reset}");

            return sb.ToString();
        }

        private static string GetColour(TimeSpan duration, double goodLimit = 10, double dangerLimit = 20)
        {
            var secs = duration.TotalSeconds;
            if (secs < goodLimit) return ConsoleConstants.Green;
            if (secs < dangerLimit) return ConsoleConstants.Yellow;
            return ConsoleConstants.Red;
        }
        #endregion
    }
}
