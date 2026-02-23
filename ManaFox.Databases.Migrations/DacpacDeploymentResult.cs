namespace ManaFox.Databases.Migrations
{
    public record DacpacDeploymentResult
    {
        public required string DacpacPath { get; init; }
        public TimeSpan Duration { get; init; }
        public string ProjectName => Path.GetFileNameWithoutExtension(DacpacPath);
    }
}
