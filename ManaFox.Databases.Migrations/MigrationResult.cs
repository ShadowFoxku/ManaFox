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

            StringBuilder sb = new($"Deploy Stats{Environment.NewLine}");
            sb.AppendLine($"{Cyan}┌{new string('─', nameWidth + 2)}┬{new string('─', 19)}┐{Reset}");
            sb.AppendLine($"{Cyan}│{Reset} {"Project".PadRight(nameWidth)} {Cyan}│{Reset} {"Duration".PadRight(17)} {Cyan}│{Reset}");
            sb.AppendLine($"{Cyan}├{new string('─', nameWidth + 2)}┼{new string('─', 19)}┤{Reset}");
            foreach (var res in DeploymentResults)
                sb.AppendLine($"{Cyan}│{Reset} {GetColour(res.Duration, goodLimit, dangerLimit)}{res.ProjectName.PadRight(nameWidth)}{Reset} {Cyan}" +
                    $"│{Reset} {GetColour(res.Duration, goodLimit, dangerLimit)}{res.Duration.ToString().PadRight(17)}{Reset} {Cyan}│{Reset}");
            sb.AppendLine($"{Cyan}└{new string('─', nameWidth + 2)}┴{new string('─', 19)}┘{Reset}");

            return sb.ToString();
        }

        const string Reset = "\u001b[0m";
        const string Cyan = "\u001b[36m";
        const string Green = "\u001b[32m";
        const string Yellow = "\u001b[33m";
        const string Red = "\u001b[31m";

        private static string GetColour(TimeSpan duration, double goodLimit = 10, double dangerLimit = 20)
        {
            var secs = duration.TotalSeconds;
            if (secs < goodLimit) return Green;
            if (secs < dangerLimit) return Yellow;
            return Red;
        }
        #endregion
    }
}
