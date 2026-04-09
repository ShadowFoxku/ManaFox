using Npgsql;

namespace ManaFox.Databases.PostgreSQL.Migrations
{
    /// <summary>
    /// Reads the current schema of a live PostgreSQL database into a DatabaseSchema snapshot.
    /// Queries information_schema and pg_catalog
    /// </summary>
    internal static class PostgresSchemaReader
    {
        public static async Task<DatabaseSchema> ReadAsync(NpgsqlConnection conn)
        {
            var schema = new DatabaseSchema();

            schema.Tables.AddRange(await ReadTablesAsync(conn));
            schema.Indexes.AddRange(await ReadIndexesAsync(conn));
            schema.ForeignKeys.AddRange(await ReadForeignKeysAsync(conn));

            return schema;
        }

        private static async Task<List<TableSchema>> ReadTablesAsync(NpgsqlConnection conn)
        {
            var tables = new Dictionary<string, TableSchema>();

            // Tables
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT table_schema, table_name
                    FROM information_schema.tables
                    WHERE table_type = 'BASE TABLE'
                      AND table_schema NOT IN ('pg_catalog', 'information_schema')
                    ORDER BY table_schema, table_name
                    """;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var t = new TableSchema
                    {
                        Schema = reader.GetString(0),
                        Name = reader.GetString(1)
                    };
                    tables[t.FullName] = t;
                }
            }

            // Columns
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT table_schema, table_name, column_name, data_type,
                           is_nullable, column_default, ordinal_position
                    FROM information_schema.columns
                    WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
                    ORDER BY table_schema, table_name, ordinal_position
                    """;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var fullName = $"{reader.GetString(0)}.{reader.GetString(1)}";
                    if (!tables.TryGetValue(fullName, out var table)) continue;

                    table.Columns.Add(new ColumnSchema
                    {
                        Name = reader.GetString(2),
                        DataType = reader.GetString(3),
                        IsNullable = reader.GetString(4) == "YES",
                        Default = reader.IsDBNull(5) ? null : reader.GetString(5),
                        OrdinalPosition = reader.GetInt32(6)
                    });
                }
            }

            // Primary keys
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT kcu.table_schema, kcu.table_name, kcu.column_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage kcu
                      ON tc.constraint_name = kcu.constraint_name
                     AND tc.table_schema    = kcu.table_schema
                    WHERE tc.constraint_type = 'PRIMARY KEY'
                      AND tc.table_schema NOT IN ('pg_catalog', 'information_schema')
                    ORDER BY kcu.table_schema, kcu.table_name, kcu.ordinal_position
                    """;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var fullName = $"{reader.GetString(0)}.{reader.GetString(1)}";
                    if (tables.TryGetValue(fullName, out var table))
                        table.PrimaryKeyColumns.Add(reader.GetString(2));
                }
            }

            return [.. tables.Values];
        }

        private static async Task<List<IndexSchema>> ReadIndexesAsync(NpgsqlConnection conn)
        {
            var indexes = new List<IndexSchema>();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT schemaname, tablename, indexname, indexdef,
                       ix.indisunique
                FROM pg_indexes pi
                JOIN pg_class c  ON c.relname  = pi.indexname
                JOIN pg_index ix ON ix.indexrelid = c.oid
                WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
                  AND indexname NOT LIKE '%_pkey'
                ORDER BY schemaname, tablename, indexname
                """;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                indexes.Add(new IndexSchema
                {
                    TableSchema = reader.GetString(0),
                    TableName = reader.GetString(1),
                    IndexName = reader.GetString(2),
                    Definition = reader.GetString(3),
                    IsUnique = reader.GetBoolean(4)
                });
            }

            return indexes;
        }

        private static async Task<List<ForeignKeySchema>> ReadForeignKeysAsync(NpgsqlConnection conn)
        {
            var fks = new List<ForeignKeySchema>();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT
                    tc.constraint_name,
                    kcu.table_schema,
                    kcu.table_name,
                    kcu.column_name,
                    ccu.table_schema AS foreign_table_schema,
                    ccu.table_name  AS foreign_table_name,
                    ccu.column_name AS foreign_column_name
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu
                  ON tc.constraint_name = kcu.constraint_name
                 AND tc.table_schema    = kcu.table_schema
                JOIN information_schema.constraint_column_usage ccu
                  ON ccu.constraint_name = tc.constraint_name
                WHERE tc.constraint_type = 'FOREIGN KEY'
                  AND tc.table_schema NOT IN ('pg_catalog', 'information_schema')
                ORDER BY tc.constraint_name
                """;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                fks.Add(new ForeignKeySchema
                {
                    ConstraintName = reader.GetString(0),
                    TableSchema = reader.GetString(1),
                    TableName = reader.GetString(2),
                    ColumnName = reader.GetString(3),
                    ForeignTableSchema = reader.GetString(4),
                    ForeignTableName = reader.GetString(5),
                    ForeignColumnName = reader.GetString(6)
                });
            }

            return fks;
        }
    }
}
