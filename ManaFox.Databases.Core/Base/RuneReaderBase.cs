using ManaFox.Databases.Core.Attributes;
using ManaFox.Databases.Core.Interfaces;
using System.Data;
using System.Reflection;
using ManaFox.Databases.Core.QueryBuilders;

namespace ManaFox.Databases.Core.Base
{
    public abstract class RuneReaderBase : IRuneReader
    {
        public abstract Task CloseAsync();
        public abstract ValueTask DisposeAsync();

        #region Execute
        public abstract Task<int> ExecuteAsync(string commandText, CommandType commandType, object? parameters);
        
        public virtual Task<int> ExecuteAsync(string commandText, CommandType commandType) 
            => ExecuteAsync(commandText, commandType, null);

        public Task<int> ExecuteAsync<T>(SearchQueryBuilder<T> query)
            => ExecuteAsync(query.Build(), CommandType.Text, query.Parameters);

        #endregion
        
        #region Single
        public abstract Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new();
        public virtual Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new()
            => QuerySingleAsync(commandText, commandType, mapFunction, null);
        
        public virtual Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new()
            => QuerySingleAsync(commandText, commandType, ReadSingleDefaultMapping<T>, parameters);

        public virtual Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType) where T : new()
            => QuerySingleAsync(commandText, commandType, ReadSingleDefaultMapping<T>, null);

        public Task<T> QuerySingleAsync<T>(SearchQueryBuilder<T> query, Func<IRowReader, T> mapFunction) where T : new()
            => QuerySingleAsync(query.Build(), CommandType.Text, mapFunction, query.Parameters);
        
        public Task<T> QuerySingleAsync<T>(SearchQueryBuilder<T> query) where T : new()
            => QuerySingleAsync(query.Build(), CommandType.Text, ReadSingleDefaultMapping<T>, query.Parameters);
        
        #endregion
        
        #region Single or Default
        protected abstract Task<T> QuerySingleOrDefaultInternalAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new();

        public async Task<T?> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new()
            => await QuerySingleOrDefaultInternalAsync(commandText, commandType, mapFunction, parameters);
        public virtual Task<T?> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new()
            => QuerySingleOrDefaultAsync(commandText, commandType, mapFunction, null);
        
        public virtual Task<T?> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new()
            => QuerySingleOrDefaultAsync(commandText, commandType, ReadSingleDefaultMapping<T>, parameters);
        
        public virtual Task<T?> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType) where T : new()
            => QuerySingleOrDefaultAsync(commandText, commandType, ReadSingleDefaultMapping<T>, null);
        
        public Task<T?> QuerySingleOrDefaultAsync<T>(SearchQueryBuilder<T> query, Func<IRowReader, T> mapFunction) where T : new()
            => QuerySingleOrDefaultAsync(query.Build(), CommandType.Text, mapFunction, query.Parameters);
        
        public Task<T?> QuerySingleOrDefaultAsync<T>(SearchQueryBuilder<T> query) where T : new()
            => QuerySingleOrDefaultAsync(query.Build(), CommandType.Text, ReadSingleDefaultMapping<T>, query.Parameters);

        #endregion
        
        #region Multiple
        public abstract Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new();
        public virtual Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new()
            => QueryMultipleAsync(commandText, commandType, mapFunction, null);
        
        public virtual Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new()
            => QueryMultipleAsync(commandText, commandType, ReadSingleDefaultMapping<T>, parameters);
        
        public virtual Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType) where T : new()
            => QueryMultipleAsync(commandText, commandType, ReadSingleDefaultMapping<T>, null);

        public Task<List<T>> QueryMultipleAsync<T>(SearchQueryBuilder<T> query) where T : new()
            => QueryMultipleAsync(query.Build(), CommandType.Text, ReadSingleDefaultMapping<T>, query.Parameters);

        public Task<List<T>> QueryMultipleAsync<T>(SearchQueryBuilder<T> query, Func<IRowReader, T> mapFunction) where T : new()
            => QueryMultipleAsync(query.Build(), CommandType.Text, mapFunction, query.Parameters);
        
        #endregion
        
        #region Helpers
        public static T ReadSingleDefaultMapping<T>(IRowReader reader) where T : new()
        {
            if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
            {
                var val = reader[0];
                if (val is DBNull) return new T();
                return (T)val;
            }

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var obj = new T();

            foreach (var prop in properties)
            {
                if (!prop.CanWrite) continue;
                if (!ShouldMapProperty(prop, out var name)) continue;
                if (!reader.HasColumn(name, out var ordinal)) continue;
                if (reader.IsDBNull(ordinal)) continue;

                var value = reader[name];
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (value != DBNull.Value && value.GetType() != targetType)
                {
                    if (TryConvert(value, targetType, out var converted))
                    {
                        prop.SetValue(obj, converted);
                    }
                    else
                    {
                        var convertedValue = Convert.ChangeType(value, prop.PropertyType);
                        prop.SetValue(obj, convertedValue);
                    }
                }
                else
                    prop.SetValue(obj, value);
            }

            return obj;
        }

        private static bool TryConvert(object value, Type toType, out object? result)
        {
            result = null;

            // Handle DateTimeOffset
            if (toType == typeof(DateTimeOffset))
            {
                switch (value)
                {
                    case DBNull or null:
                        return true;
                    case DateTime dateTime:
                        result = new DateTimeOffset(dateTime);
                        return true;
                    case string dateString when DateTimeOffset.TryParse(dateString, out var offset):
                        result = offset;
                        return true;
                }
            }

            // Handle Enum types
            if (!toType.IsEnum) return false;
            switch (value)
            {
                case string enumString:
                    try
                    {
                        result = Enum.Parse(toType, enumString, ignoreCase: true);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                case int or long or byte or sbyte or short or ushort or uint or ulong:
                    try
                    {
                        result = Enum.ToObject(toType, value);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                default:
                    return false;
            }
        }

        protected static bool ShouldMapProperty(PropertyInfo prop, out string mapAs)
        {
            mapAs = prop.Name;
            var shouldIgnore = prop.GetCustomAttribute<IgnoreRuneAttribute>();
            if (shouldIgnore != null)
                return false;

            var attrib = prop.GetCustomAttribute<RuneNameAttribute>();
            if (attrib != null)
                mapAs = attrib?.ColumnName ?? mapAs;

            return true;
        }
        #endregion
    }
}
