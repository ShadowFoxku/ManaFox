using System.Text;

namespace ManaFox.Databases.PostgreSQL.Migrations
{
    /// <summary>
    /// Diffs a desired schema (shadow) against a current schema (target) and
    /// produces the SQL delta needed to bring the target in line with the desired state.
    /// </summary>
    internal class SchemaDiffer(MigratorOptions options)
    {
        private readonly MigratorOptions _options = options;

        public string GenerateMigration(DatabaseSchema desired, DatabaseSchema current)
        {
            var sb = new StringBuilder();

            AppendTableChanges(sb, desired, current);
            AppendIndexChanges(sb, desired, current);
            AppendForeignKeyChanges(sb, desired, current);

            return sb.ToString().Trim();
        }

        #region Tables & Columns

        private void AppendTableChanges(StringBuilder sb, DatabaseSchema desired, DatabaseSchema current)
        {
            var currentTables = current.Tables.ToDictionary(t => t.FullName);
            var desiredTables = desired.Tables.ToDictionary(t => t.FullName);

            // New tables
            foreach (var table in desired.Tables)
            {
                if (!currentTables.ContainsKey(table.FullName))
                    AppendCreateTable(sb, table);
            }

            // Altered tables (column changes)
            foreach (var desiredTable in desired.Tables)
            {
                if (!currentTables.TryGetValue(desiredTable.FullName, out var currentTable))
                    continue;

                AppendColumnChanges(sb, desiredTable, currentTable);
            }

            // Dropped tables — only if option is set, matching DacFX DropObjectsNotInSource
            if (_options.DropObjectsNotInSource)
            {
                foreach (var table in current.Tables)
                {
                    if (!desiredTables.ContainsKey(table.FullName))
                        sb.AppendLine($"DROP TABLE IF EXISTS \"{table.Schema}\".\"{table.Name}\" CASCADE;");
                }
            }
        }

        private void AppendCreateTable(StringBuilder sb, TableSchema table)
        {
            sb.AppendLine($"CREATE TABLE IF NOT EXISTS \"{table.Schema}\".\"{table.Name}\" (");

            var colLines = table.Columns
                .OrderBy(c => c.OrdinalPosition)
                .Select(c => $"    {FormatColumnDefinition(c)}")
                .ToList();

            if (table.PrimaryKeyColumns.Count > 0)
            {
                var pkCols = string.Join(", ", table.PrimaryKeyColumns.Select(c => $"\"{c}\""));
                colLines.Add($"    PRIMARY KEY ({pkCols})");
            }

            sb.AppendLine(string.Join(",\n", colLines));
            sb.AppendLine(");");
            sb.AppendLine();
        }

        private void AppendColumnChanges(StringBuilder sb, TableSchema desired, TableSchema current)
        {
            var currentCols = current.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
            var desiredCols = desired.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

            // New columns
            foreach (var col in desired.Columns)
            {
                if (!currentCols.ContainsKey(col.Name))
                    sb.AppendLine($"ALTER TABLE \"{desired.Schema}\".\"{desired.Name}\" ADD COLUMN IF NOT EXISTS {FormatColumnDefinition(col)};");
            }

            // Modified columns (type or nullability changed)
            foreach (var desiredCol in desired.Columns)
            {
                if (!currentCols.TryGetValue(desiredCol.Name, out var currentCol)) continue;

                if (!string.Equals(desiredCol.DataType, currentCol.DataType, StringComparison.OrdinalIgnoreCase))
                    sb.AppendLine($"ALTER TABLE \"{desired.Schema}\".\"{desired.Name}\" ALTER COLUMN \"{desiredCol.Name}\" TYPE {desiredCol.DataType};");

                if (desiredCol.IsNullable != currentCol.IsNullable)
                {
                    var nullClause = desiredCol.IsNullable ? "DROP NOT NULL" : "SET NOT NULL";
                    sb.AppendLine($"ALTER TABLE \"{desired.Schema}\".\"{desired.Name}\" ALTER COLUMN \"{desiredCol.Name}\" {nullClause};");
                }
            }

            // Dropped columns — only if configured
            if (_options.DropObjectsNotInSource)
            {
                foreach (var col in current.Columns)
                {
                    if (!desiredCols.ContainsKey(col.Name))
                        sb.AppendLine($"ALTER TABLE \"{desired.Schema}\".\"{desired.Name}\" DROP COLUMN IF EXISTS \"{col.Name}\";");
                }
            }
        }

        #endregion

        #region Indexes

        private void AppendIndexChanges(StringBuilder sb, DatabaseSchema desired, DatabaseSchema current)
        {
            var currentIdx = current.Indexes.ToDictionary(i => i.IndexName, StringComparer.OrdinalIgnoreCase);
            var desiredIdx = desired.Indexes.ToDictionary(i => i.IndexName, StringComparer.OrdinalIgnoreCase);

            // New indexes
            foreach (var idx in desired.Indexes)
            {
                if (!currentIdx.ContainsKey(idx.IndexName))
                    sb.AppendLine($"{idx.Definition};");
            }

            // Dropped indexes
            if (_options.DropObjectsNotInSource)
            {
                foreach (var idx in current.Indexes)
                {
                    if (!desiredIdx.ContainsKey(idx.IndexName))
                        sb.AppendLine($"DROP INDEX IF EXISTS \"{idx.TableSchema}\".\"{idx.IndexName}\";");
                }
            }
        }

        #endregion

        #region Foreign Keys

        private void AppendForeignKeyChanges(StringBuilder sb, DatabaseSchema desired, DatabaseSchema current)
        {
            var currentFks = current.ForeignKeys.ToDictionary(f => f.ConstraintName, StringComparer.OrdinalIgnoreCase);
            var desiredFks = desired.ForeignKeys.ToDictionary(f => f.ConstraintName, StringComparer.OrdinalIgnoreCase);

            // New foreign keys
            foreach (var fk in desired.ForeignKeys)
            {
                if (!currentFks.ContainsKey(fk.ConstraintName))
                {
                    sb.AppendLine(
                        $"ALTER TABLE \"{fk.TableSchema}\".\"{fk.TableName}\" " +
                        $"ADD CONSTRAINT \"{fk.ConstraintName}\" " +
                        $"FOREIGN KEY (\"{fk.ColumnName}\") " +
                        $"REFERENCES \"{fk.ForeignTableSchema}\".\"{fk.ForeignTableName}\" (\"{fk.ForeignColumnName}\");");
                }
            }

            // Dropped foreign keys
            if (_options.DropObjectsNotInSource)
            {
                foreach (var fk in current.ForeignKeys)
                {
                    if (!desiredFks.ContainsKey(fk.ConstraintName))
                        sb.AppendLine($"ALTER TABLE \"{fk.TableSchema}\".\"{fk.TableName}\" DROP CONSTRAINT IF EXISTS \"{fk.ConstraintName}\";");
                }
            }
        }

        #endregion

        #region Helpers

        private static string FormatColumnDefinition(ColumnSchema col)
        {
            var sb = new StringBuilder();
            sb.Append($"\"{col.Name}\" {col.DataType}");

            if (!string.IsNullOrWhiteSpace(col.Default))
                sb.Append($" DEFAULT {col.Default}");

            if (!col.IsNullable)
                sb.Append(" NOT NULL");

            return sb.ToString();
        }

        #endregion
    }
}
