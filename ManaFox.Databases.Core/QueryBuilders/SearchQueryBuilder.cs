using System.Linq.Expressions;
using System.Text;

namespace ManaFox.Databases.Core.QueryBuilders;


public class SearchQueryBuilder<T>
{
    private const int MaxPageSize = 100;

    private readonly Dictionary<Type, string> _aliases = new();
    private readonly List<string> _where = new();
    private readonly Dictionary<string, object?> _params = new();

    private string _main = "";
    private string? _orderBy;
    private int? _limit;
    private int? _offset;
    
    public SearchQueryBuilder(string? tableAlias = null)
    {
        if (tableAlias != null) _aliases[typeof(T)] = tableAlias;
    }

    public IReadOnlyDictionary<string, object?> Parameters => _params;

    public SearchQueryBuilder<T> WithAlias<TTable>(string alias)
    {
        _aliases[typeof(TTable)] = alias;
        return this;
    }

    /// <summary>
    /// Internal only, do not use user values here
    /// </summary>
    public SearchQueryBuilder<T> WithMainQuery(string mainQuery)
    {
        _main = mainQuery;
        return this;
    }

    public SearchQueryBuilder<T> WhereEquals(Expression<Func<T, object?>> col, object? value)
        => WhereEquals<T>(col, value);

    public SearchQueryBuilder<T> WhereEquals<TTable>(Expression<Func<TTable, object?>> col, object? value)
        => WhereEqualsInternal(col, value);

    public SearchQueryBuilder<T> WhereEqualsIfNotNull(Expression<Func<T, object?>> col, object? value)
        => WhereEqualsIfNotNull<T>(col, value);
    
    public SearchQueryBuilder<T> WhereEqualsIfNotNull<TTable>(Expression<Func<TTable, object?>> col, object? value)
    {
        if (value is null) return this;
        return WhereEqualsInternal(col, value);
    }

    private SearchQueryBuilder<T> WhereEqualsInternal<TTable>(Expression<Func<TTable, object?>> col, object? value)
    {
        var colName = GetColumnRef(Columns.Ref(col));

        if (value is null)
        {
            _where.Add($"({colName} IS NULL)");
            return this;
        }

        var p = AddParam(value, colName.Split('.').LastOrDefault());
        _where.Add($"({colName} = @{p})");

        return this;
    }

    public SearchQueryBuilder<T> WhereContains(Expression<Func<T, object?>> col, string? rawTerm)
        => WhereAnyContains(rawTerm, Columns.Ref(col));
    
    public SearchQueryBuilder<T> WhereAnyContains(string? rawTerm, params Expression<Func<T, object?>>[] cols)
        => WhereAnyContains(rawTerm, [.. cols.Select(Columns.Ref)]);

    public SearchQueryBuilder<T> WhereContains<TTable>(
        Expression<Func<TTable, object?>> col, string? rawTerm)
        => WhereAnyContains(rawTerm, Columns.Ref(col));
    
    public SearchQueryBuilder<T> WhereAnyContains<TTable>(string? rawTerm, params Expression<Func<TTable, object?>>[] cols)
        => WhereAnyContains(rawTerm, [.. cols.Select(Columns.Ref)]);
    
    public SearchQueryBuilder<T> WhereAnyContains(string? rawTerm, params ColumnRef[] cols)
    {
        var pattern = SafeSearchTerm.ToContainsPattern(rawTerm);
        if (pattern == null || cols.Length == 0) return this;

        var p = AddParam(pattern, "search_term");
        var parts = cols.Select(c => $"{GetColumnRef(c)} ILIKE @{p} ESCAPE '\\'");
        _where.Add("(" + string.Join(" OR ", parts) + ")");
        return this;
    }

    /// <summary>
    /// Internal only, do not inject user given values here
    /// </summary>
    public SearchQueryBuilder<T> WithAndCondition(string condition)
    {
        _where.Add($"({condition})");
        return this;
    }
    
    public SearchQueryBuilder<T> WithAndCondition(string condition, params (string name, object? val)[] values)
    {
        _where.Add($"({condition})");

        foreach (var v in values)
            AddParam(v.val, v.name);
        
        return this;
    }
    
    public SearchQueryBuilder<T> WithOrderBy(Expression<Func<T, object?>> col, bool descending = false)
        => WithOrderBy<T>(col, descending);

    public SearchQueryBuilder<T> WithOrderBy<TTable>(Expression<Func<TTable, object?>> col, bool descending = false)
    {
        _orderBy = GetColumnRef(Columns.Ref(col)) + (descending ? " DESC" : "");
        return this;
    }
    
    public SearchQueryBuilder<T> WithPagination(int pageNumber, int pageSize)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var offset = ((long)pageNumber - 1) * pageSize;

        if (offset > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(pageNumber));

        _limit = pageSize;
        _offset = (int)offset;

        return this;
    }

    public string Build()
    {
        if (_main.Length == 0)
            throw new InvalidOperationException("WithMainQuery must be called.");

        var sb = new StringBuilder(_main.TrimEnd().TrimEnd(';')).AppendLine();

        if (_where.Count > 0)
            sb.Append("WHERE ").AppendLine(string.Join("\nAND ", _where));

        if (_orderBy != null)
            sb.Append("ORDER BY ").AppendLine(_orderBy);

        if (_limit != null)
        {
            if (_orderBy == null)
                throw new InvalidOperationException("Pagination requires an ORDER BY.");

            _params["limit"] = _limit;
            _params["offset"] = _offset;
            sb.AppendLine("LIMIT @limit OFFSET @offset");
        }

        return sb.Append(';').ToString();
    }

    public string BuildTotalCount(string countQuery)
    {        
        if (countQuery.Length == 0)
            throw new ArgumentNullException(nameof(countQuery));

        var sb = new StringBuilder(countQuery.TrimEnd().TrimEnd(';')).AppendLine();

        if (_where.Count > 0)
            sb.Append("WHERE ").AppendLine(string.Join("\nAND ", _where));

        return sb.Append(';').ToString();
    }

    private string GetColumnRef(ColumnRef c)
    {
        if (_aliases.TryGetValue(c.Table, out var alias))
            return $"{alias}.{c.Name}";

        if (_aliases.Count == 0)
            return c.Name;

        throw new InvalidOperationException(
            $"No alias registered for {c.Table.Name}. " +
            $"Pass it to the constructor or call WithAlias<{c.Table.Name}>(...).");
    }

    private string AddParam(object? value, string? desiredName)
    {
        desiredName = string.IsNullOrWhiteSpace(desiredName)
            ? $"p{_params.Count}"
            : desiredName;

        if (desiredName.Equals("limit", StringComparison.InvariantCultureIgnoreCase)
            || desiredName.Equals("offset", StringComparison.InvariantCultureIgnoreCase))
            desiredName = $"c{desiredName}";

        if (_params.TryGetValue(desiredName, out var existing))
        {
            if (Equals(value, existing))
                return desiredName;

            var baseName = desiredName;
            var suffix = 1;

            while (_params.ContainsKey(desiredName))
                desiredName = $"{baseName}_{suffix++}";
        }

        _params.Add(desiredName, value);
        return desiredName;
    }
}