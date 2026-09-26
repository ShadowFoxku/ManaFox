using Npgsql;

namespace ManaFox.Databases.PostgreSQL.Migrations
{
    internal static class SqlBatchExecutor
    {
        public static async Task<List<(string Source, string Sql)>> LoadStatementsAsync(IEnumerable<string> filePaths)
        {
            var statements = new List<(string Source, string Sql)>();

            foreach (var file in filePaths)
            {
                var sql = await File.ReadAllTextAsync(file);
                if (string.IsNullOrWhiteSpace(sql)) continue;

                foreach (var stmt in SplitStatements(sql))
                    statements.Add((Path.GetFileName(file), stmt));
            }

            return statements;
        }
        
        public static async Task ExecuteWithDependencyRetryAsync(
            NpgsqlConnection connection,
            IReadOnlyList<(string Source, string Sql)> statements,
            NpgsqlTransaction? transaction = null)
        {
            var remaining = statements;

            while (remaining.Count > 0)
            {
                var stillFailing = new List<(string Source, string Sql)>();
                var lastErrors = new Dictionary<string, Exception>();

                foreach (var (source, stmt) in remaining)
                {
                    try
                    {
                        await using var cmd = connection.CreateCommand();
                        cmd.CommandText = stmt;
                        if (transaction != null)
                            cmd.Transaction = transaction;

                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
                    {
                        stillFailing.Add((source, stmt));
                        lastErrors[stmt] = ex;
                    }
                    catch (NpgsqlException ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to apply SQL from '{source}': {ex.Message}\nStatement:\n{stmt.Trim()}", ex);
                    }
                }

                if (stillFailing.Count == remaining.Count)
                {
                    var details = string.Join("\n\n", stillFailing.Select(s =>
                        $"[{s.Source}]\n{s.Sql.Trim()}\n  -> {lastErrors[s.Sql].Message}"));

                    throw new InvalidOperationException("Ran into unexpected issue: \n\n" + details);
                }

                remaining = stillFailing;
            }
        }
        
        public static IEnumerable<string> SplitStatements(string sql)
        {
            var statements = new List<string>();
            var current = new System.Text.StringBuilder();

            int i = 0;
            while (i < sql.Length)
            {
                char c = sql[i];

                // Single-quoted string literal
                if (c == '\'')
                {
                    int start = i;
                    i++;
                    while (i < sql.Length && !(sql[i] == '\'' && (i + 1 >= sql.Length || sql[i + 1] != '\'')))
                    {
                        if (sql[i] == '\'' && i + 1 < sql.Length && sql[i + 1] == '\'') i++; // escaped ''
                        i++;
                    }
                    i = Math.Min(i + 1, sql.Length);
                    current.Append(sql, start, i - start);
                    continue;
                }

                // Double-quoted identifier
                if (c == '"')
                {
                    int start = i;
                    i++;
                    while (i < sql.Length && sql[i] != '"') i++;
                    i = Math.Min(i + 1, sql.Length);
                    current.Append(sql, start, i - start);
                    continue;
                }

                // Dollar-quoted body, e.g. $$ ... $$ or $tag$ ... $tag$
                if (c == '$')
                {
                    int tagEnd = sql.IndexOf('$', i + 1);
                    if (tagEnd != -1)
                    {
                        var tag = sql.Substring(i, tagEnd - i + 1); // includes both $
                        var closeIdx = sql.IndexOf(tag, tagEnd + 1, StringComparison.Ordinal);
                        if (closeIdx != -1)
                        {
                            int end = closeIdx + tag.Length;
                            current.Append(sql, i, end - i);
                            i = end;
                            continue;
                        }
                    }
                }

                if (c == ';')
                {
                    var stmt = current.ToString();
                    if (!string.IsNullOrWhiteSpace(stmt))
                        statements.Add(stmt);
                    current.Clear();
                    i++;
                    continue;
                }

                current.Append(c);
                i++;
            }

            var tail = current.ToString();
            if (!string.IsNullOrWhiteSpace(tail))
                statements.Add(tail);

            return statements;
        }
    }
}
