using System.Text;
using System.Text.RegularExpressions;

namespace ManaFox.Databases.PostgreSQL.Migrations
{
    internal static class TableSqlSplitter
    {
        private static readonly Regex CreateTableHeader = new(
            @"^\s*CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?<name>[^\(]+)\(",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ReferencesClause = new(
            @"\bREFERENCES\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TableLevelForeignKey = new(
            @"^\s*(?:CONSTRAINT\s+(?<name>""?\w+""?)\s+)?FOREIGN\s+KEY\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool LooksLikeCreateTable(string statement) =>
            Regex.IsMatch(statement, @"^\s*CREATE\s+TABLE\b", RegexOptions.IgnoreCase);
        
        public static (string TableSql, List<string> ConstraintSql) ExtractForeignKeys(string createTableStatement)
        {
            var headerMatch = CreateTableHeader.Match(createTableStatement);
            if (!headerMatch.Success)
                return (createTableStatement, []);

            var qualifiedName = headerMatch.Groups["name"].Value.Trim();

            int bodyStart = headerMatch.Index + headerMatch.Length - 1; // the opening '('
            int bodyEnd = FindMatchingParenthesis(createTableStatement, bodyStart);
            if (bodyEnd == -1)
                return (createTableStatement, []); // unparseable, leave untouched

            var body = createTableStatement.Substring(bodyStart + 1, bodyEnd - bodyStart - 1);
            var tail = createTableStatement.Substring(bodyEnd + 1); // e.g. "PARTITION BY ..." after the column list

            var items = SplitTopLevel(body);
            var keptItems = new List<string>();
            var constraints = new List<string>();
            int autoNameCounter = 1;

            foreach (var rawItem in items)
            {
                var item = rawItem.Trim();
                if (item.Length == 0) continue;

                var tableFkMatch = TableLevelForeignKey.Match(item);
                if (tableFkMatch.Success)
                {
                    var constraintName = tableFkMatch.Groups["name"].Success
                        ? tableFkMatch.Groups["name"].Value.Trim('"')
                        : $"{SafeIdentifier(qualifiedName)}_fkey_{autoNameCounter++}";

                    var fkKeywordIndex = item.IndexOf("FOREIGN", tableFkMatch.Index, StringComparison.OrdinalIgnoreCase);
                    var clause = item[fkKeywordIndex..].Trim(); // "FOREIGN KEY (...) REFERENCES ... [ON ...]"

                    constraints.Add($"ALTER TABLE {qualifiedName} ADD CONSTRAINT \"{constraintName}\" {clause};");
                    continue;
                }

                if (ReferencesClause.IsMatch(item))
                {
                    var refMatch = ReferencesClause.Match(item);
                    var columnDef = item[..refMatch.Index].TrimEnd();
                    var refClause = item[refMatch.Index..].Trim();

                    var columnName = columnDef
                        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0]
                        .Trim('"');
                    var constraintName = $"{SafeIdentifier(qualifiedName)}_{SafeIdentifier(columnName)}_fkey";

                    keptItems.Add(columnDef);
                    constraints.Add(
                        $"ALTER TABLE {qualifiedName} ADD CONSTRAINT \"{constraintName}\" FOREIGN KEY (\"{columnName}\") {refClause};");
                    continue;
                }

                keptItems.Add(item);
            }

            if (constraints.Count == 0)
                return (createTableStatement, []); // nothing to extract

            var rebuilt = $"CREATE TABLE IF NOT EXISTS {qualifiedName} (\n    " +
                          string.Join(",\n    ", keptItems) +
                          "\n)" + tail;

            return (rebuilt, constraints);
        }

        private static string SafeIdentifier(string raw) =>
            raw.Replace("\"", "").Replace(".", "_").Trim();
        
        private static List<string> SplitTopLevel(string body)
        {
            var items = new List<string>();
            var current = new StringBuilder();
            int depth = 0;
            int i = 0;

            while (i < body.Length)
            {
                char c = body[i];

                if (c == '\'')
                {
                    int start = i;
                    i++;
                    while (i < body.Length && !(body[i] == '\'' && (i + 1 >= body.Length || body[i + 1] != '\'')))
                    {
                        if (body[i] == '\'' && i + 1 < body.Length && body[i + 1] == '\'') i++;
                        i++;
                    }
                    i = Math.Min(i + 1, body.Length);
                    current.Append(body, start, i - start);
                    continue;
                }

                if (c == '"')
                {
                    int start = i;
                    i++;
                    while (i < body.Length && body[i] != '"') i++;
                    i = Math.Min(i + 1, body.Length);
                    current.Append(body, start, i - start);
                    continue;
                }

                if (c == '(')
                {
                    depth++;
                    current.Append(c);
                    i++;
                    continue;
                }

                if (c == ')')
                {
                    depth--;
                    current.Append(c);
                    i++;
                    continue;
                }

                if (c == ',' && depth == 0)
                {
                    items.Add(current.ToString());
                    current.Clear();
                    i++;
                    continue;
                }

                current.Append(c);
                i++;
            }

            if (current.Length > 0)
                items.Add(current.ToString());

            return items;
        }

        private static int FindMatchingParenthesis(string s, int openIndex)
        {
            int depth = 0;
            int i = openIndex;

            while (i < s.Length)
            {
                char c = s[i];

                if (c == '\'')
                {
                    i++;
                    while (i < s.Length && !(s[i] == '\'' && (i + 1 >= s.Length || s[i + 1] != '\'')))
                    {
                        if (s[i] == '\'' && i + 1 < s.Length && s[i + 1] == '\'') i++;
                        i++;
                    }
                    i = Math.Min(i + 1, s.Length);
                    continue;
                }

                if (c == '"')
                {
                    i++;
                    while (i < s.Length && s[i] != '"') i++;
                    i = Math.Min(i + 1, s.Length);
                    continue;
                }

                if (c == '(')
                {
                    depth++;
                    i++;
                    continue;
                }

                if (c == ')')
                {
                    depth--;
                    if (depth == 0) return i;
                    i++;
                    continue;
                }

                i++;
            }

            return -1;
        }
    }
}
