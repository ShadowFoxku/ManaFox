using Microsoft.Extensions.Logging;
using Npgsql;

namespace ManaFox.Databases.PostgreSQL.Migrations
{
    /// <summary>
    /// Reads the current schema of a live PostgreSQL database into a DatabaseSchema snapshot.
    /// Queries information_schema and pg_catalog
    /// </summary>
    internal static class PostgresSchemaReader
    {
        public static async Task<DatabaseSchema> ReadAsync(NpgsqlConnection conn, MigratorOptions? options = null)
        {
            var excludeSchemas = options?.ExcludeSchemas ?? [];
            var schema = new DatabaseSchema();

            schema.Tables.AddRange((await ReadTablesAsync(conn))
                .Where(t => !excludeSchemas.Contains(t.Schema, StringComparer.OrdinalIgnoreCase)));
            schema.Indexes.AddRange((await ReadIndexesAsync(conn))
                .Where(i => !excludeSchemas.Contains(i.TableSchema, StringComparer.OrdinalIgnoreCase)));
            schema.ForeignKeys.AddRange((await ReadForeignKeysAsync(conn))
                .Where(f => !excludeSchemas.Contains(f.TableSchema, StringComparer.OrdinalIgnoreCase)));
            return schema;
        }

        private static async Task<List<TableSchema>> ReadTablesAsync(NpgsqlConnection conn)
        {
            var tables = new Dictionary<string, TableSchema>();

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT table_schema, table_name, column_name, data_type,
                           is_nullable, column_default, ordinal_position,
                           character_maximum_length, numeric_precision, numeric_scale
                    FROM information_schema.columns
                    WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
                    ORDER BY table_schema, table_name, ordinal_position
                    """;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var tableSchema = reader.GetString(0);
                    var tableName = reader.GetString(1);
                    var fullName = $"{tableSchema}.{tableName}";

                    if (!tables.TryGetValue(fullName, out var table))
                    {
                        table = new TableSchema { Schema = tableSchema, Name = tableName };
                        tables[fullName] = table;
                    }

                    table.Columns.Add(new ColumnSchema
                    {
                        Name = reader.GetString(2),
                        DataType = reader.GetString(3),
                        IsNullable = reader.GetString(4) == "YES",
                        Default = reader.IsDBNull(5) ? null : reader.GetString(5),
                        OrdinalPosition = reader.GetInt32(6),
                        CharacterMaxLength = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                        NumericPrecision = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                        NumericScale = reader.IsDBNull(9) ? null : reader.GetInt32(9)
                    });
                }
            }

            // Primary keys — unchanged, just needs `tables` to actually be populated now
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
