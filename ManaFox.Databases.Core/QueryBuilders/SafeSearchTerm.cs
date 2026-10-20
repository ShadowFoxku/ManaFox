namespace ManaFox.Databases.Core.QueryBuilders;

public static class SafeSearchTerm
{
    private const int MaxLength = 100;

    public static string? ToContainsPattern(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var term = raw.Replace("\0", "").Trim();
        if (term.Length > MaxLength) term = term[..MaxLength];
        if (term.Length == 0) return null;

        var escaped = term
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

        return $"%{escaped}%";
    }
}