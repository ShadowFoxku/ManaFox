namespace ManaFox.Databases.PostgreSQL.Migrations
{
    /// <summary>
    /// In-memory snapshot of a PostgreSQL database schema.
    /// </summary>
    internal class DatabaseSchema
    {
        public List<TableSchema> Tables { get; init; } = [];
        public List<IndexSchema> Indexes { get; init; } = [];
        public List<ForeignKeySchema> ForeignKeys { get; init; } = [];
        public List<string> Views { get; init; } = [];
        public List<string> Functions { get; init; } = [];
    }

    internal class TableSchema
    {
        public required string Schema { get; init; }
        public required string Name { get; init; }
        public List<ColumnSchema> Columns { get; init; } = [];
        public List<string> PrimaryKeyColumns { get; init; } = [];
        public string FullName => $"{Schema}.{Name}";
    }

    internal class ColumnSchema
    {
        public required string Name { get; init; }
        public required string DataType { get; init; }
        public bool IsNullable { get; init; }
        public string? Default { get; init; }
        public int OrdinalPosition { get; init; }
    }

    internal class IndexSchema
    {
        public required string TableSchema { get; init; }
        public required string TableName { get; init; }
        public required string IndexName { get; init; }
        public required string Definition { get; init; }
        public bool IsUnique { get; init; }
    }

    internal class ForeignKeySchema
    {
        public required string ConstraintName { get; init; }
        public required string TableSchema { get; init; }
        public required string TableName { get; init; }
        public required string ColumnName { get; init; }
        public required string ForeignTableSchema { get; init; }
        public required string ForeignTableName { get; init; }
        public required string ForeignColumnName { get; init; }
    }
}
