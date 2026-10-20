using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using ManaFox.Databases.Core.Attributes;

namespace ManaFox.Databases.Core.QueryBuilders;

public readonly record struct ColumnRef(Type Table, string Name);

public static class Columns
{
    private static readonly ConcurrentDictionary<MemberInfo, string> Cache = new();

    public static string Of<TTable>(Expression<Func<TTable, object?>> selector)
    {
        var member = GetMember(selector.Body);

        return Cache.GetOrAdd(member, m =>
            m.GetCustomAttribute<RuneNameAttribute>()?.ColumnName
            ?? throw new InvalidOperationException(
                $"{m.DeclaringType?.Name}.{m.Name} has no [RuneName]"));
    }

    public static ColumnRef Ref<TTable>(Expression<Func<TTable, object?>> selector)
        => new(typeof(TTable), Of(selector));

    private static MemberInfo GetMember(Expression body)
    {
        if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
            body = u.Operand;

        if (body is MemberExpression { Member: PropertyInfo p })
            return p;

        throw new ArgumentException("Selector must be a plain property access, e.g. u => u.Username");
    }
}