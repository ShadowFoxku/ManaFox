using ManaFox.Databases.Core.Attributes;
using ManaFox.Databases.Core.Interfaces;
using System.Data;
using System.Reflection;

namespace ManaFox.Databases.Core.Base
{
    public abstract class RuneReaderBase : IRuneReader
    {
        public abstract Task CloseAsync();
        public abstract Task<int> ExecuteAsync(string commandText, CommandType commandType, object? parameters);
        public abstract Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new();
        public abstract Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new();
        public abstract Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new();

        public virtual Task<int> ExecuteAsync(string commandText, CommandType commandType)
        {
            return ExecuteAsync(commandText, commandType, null);
        }

        public virtual Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new()
        {
            return QueryMultipleAsync(commandText, commandType, ReadSingleDefaultMapping<T>, parameters);
        }

        public virtual Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType) where T : new()
        {
            return QueryMultipleAsync(commandText, commandType, ReadSingleDefaultMapping<T>, null);
        }

        public virtual Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new()
        {
            return QueryMultipleAsync(commandText, commandType, mapFunction, null);
        }

        public virtual Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new()
        {
            return QuerySingleAsync(commandText, commandType, ReadSingleDefaultMapping<T>, parameters);
        }

        public virtual Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType) where T : new()
        {
            return QuerySingleAsync(commandText, commandType, ReadSingleDefaultMapping<T>, null);
        }

        public virtual Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new()
        {
            return QuerySingleAsync(commandText, commandType, mapFunction, null);
        }

        public virtual Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new()
        {
            return QuerySingleOrDefaultAsync(commandText, commandType, ReadSingleDefaultMapping<T>, parameters);
        }

        public virtual Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType) where T : new()
        {
            return QuerySingleOrDefaultAsync(commandText, commandType, ReadSingleDefaultMapping<T>, null);
        }

        public virtual Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new()
        {
            return QuerySingleOrDefaultAsync(commandText, commandType, mapFunction, null);
        }

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
                if (value != null && value.GetType() != targetType)
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

        public static bool TryConvert(object value, Type toType, out object? result)
        {
            result = null;

            // Handle DateTimeOffset
            if (toType == typeof(DateTimeOffset))
            {
                if (value is DBNull || value is null)
                {
                    return true;
                }

                if (value is DateTime dateTime)
                {
                    result = new DateTimeOffset(dateTime);
                    return true;
                }

                if (value is string dateString && DateTimeOffset.TryParse(dateString, out var offset))
                {
                    result = offset;
                    return true;
                }
            }

            // Handle Enum types
            if (toType.IsEnum)
            {
                if (value is string enumString)
                {
                    try
                    {
                        result = Enum.Parse(toType, enumString, ignoreCase: true);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                if (value is int or long or byte or sbyte or short or ushort or uint or ulong)
                {
                    try
                    {
                        result = Enum.ToObject(toType, value);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        public static bool ShouldMapProperty(PropertyInfo prop, out string mapAs)
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

        public abstract ValueTask DisposeAsync();
    }
}
