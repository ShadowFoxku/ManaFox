namespace ManaFox.Databases.PostgreSQL.Migrations
{
    /// <summary>
    /// Controls the behaviour of schema diffing and deployment.
    /// </summary>
    public class MigratorOptions
    {
        /// <summary>
        /// When true, tables/columns/indexes present in the target DB but absent
        /// from the SQL definition files will be dropped. Defaults to false
        /// </summary>
        public bool DropObjectsNotInSource { get; init; } = false;

        /// <summary>
        /// When true, the migration will be aborted if any destructive operation
        /// (DROP TABLE, DROP COLUMN, ALTER TYPE) would cause data loss.
        /// </summary>
        public bool BlockOnPossibleDataLoss { get; init; } = true;

        /// <summary>
        /// Schemas to completely ignore during diffing. Useful for application schemas you don't own.
        /// </summary>
        public IReadOnlyList<string> ExcludeSchemas { get; init; } = [];

        public static MigratorOptions Default => new();
    }
}
