namespace ManaFox.Databases.Migrations
{
    public record MigrationResult
    {
        public required string DatabaseName { get; init; }
        public bool DatabaseExists { get; init; }
        public required IReadOnlyList<DacpacDeploymentResult> DeploymentResults { get; init; }
        public TimeSpan Duration { get; init; }
        public int DacpacsDeployed => DeploymentResults.Count;
    }
}
