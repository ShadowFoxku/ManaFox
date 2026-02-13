using ManaFox.Databases.TSQL.Attributes;
using ManaFox.Databases.TSQL.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;

namespace ManaFox.Databases.TSQL.Models
{
    public class RuneReader(SqlConnection conn) : IRuneReader, IDisposable
    {
        private readonly SqlConnection Connection = conn;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (Connection.State != ConnectionState.Closed)
                Close();
        }

        private void ValidateConnection()
        {
            if (Connection.State != ConnectionState.Open)
                throw new InvalidOperationException("The database connection is not open.");
        }

        ~RuneReader()
        {
            Dispose();
        }

        #region Async
        public async Task<int> ExecuteAsync(string commandText, CommandType commandType, object? parameters)
        {
            var com = CreateCommand(commandText, commandType, parameters);
            return await com.ExecuteNonQueryAsync();
        }

        public Task<int> ExecuteAsync(string commandText, CommandType commandType)
        {
            return ExecuteAsync(commandText, commandType, null);
        }

        public Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new()
        {
            return QueryMultipleAsync(commandText, commandType, ReadSingleDefaultMapping<T>, parameters);
        }

        public async Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType) where T : new()
        {
            return await QueryMultipleAsync(commandText, commandType, ReadSingleDefaultMapping<T>, null);
        }

        public Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction) where T : new()
        {
            return QueryMultipleAsync(commandText, commandType, mapFunction, null);
        }

        public async Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction, object? parameters) where T : new()
        {
            var com = CreateCommand(commandText, commandType, parameters);

            var items = new List<T>();
            using var results = await com.ExecuteReaderAsync();
            while (await results.ReadAsync())
            {
                T obj = ReadSingleDefaultMapping<T>(results);
                items.Add(obj);
            }

            return items;
        }

        public Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new()
        {
            return QuerySingleAsync(commandText, commandType, ReadSingleDefaultMapping<T>, parameters);
        }

        public Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType) where T : new()
        {
            return QuerySingleAsync(commandText, commandType, ReadSingleDefaultMapping<T>, null);
        }

        public Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction) where T : new()
        {
            return QuerySingleAsync(commandText, commandType, mapFunction, null);
        }

        public async Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction, object? parameters) where T : new()
        {
            var com = CreateCommand(commandText, commandType, parameters);
            var result = new T();

            using var results = await com.ExecuteReaderAsync();
            if (await results.ReadAsync())
            {
                result = mapFunction(results);
            }

            return result;
        }

        public async Task CloseAsync()
        {
            await Connection.CloseAsync();
            await Connection.DisposeAsync();
        }
        #endregion Async

        #region Sync
        public int Execute(string commandText, CommandType commandType, object? parameters)
        {
            var com = CreateCommand(commandText, commandType, parameters);
            return com.ExecuteNonQuery();
        }

        public int Execute(string commandText, CommandType commandType)
        {
            return Execute(commandText, commandType, null);
        }

        public List<T> QueryMultiple<T>(string commandText, CommandType commandType, object? parameters) where T : new()
        {
            return QueryMultiple(commandText, commandType, ReadSingleDefaultMapping<T>, parameters);
        }

        public  List<T> QueryMultiple<T>(string commandText, CommandType commandType) where T : new()
        {
            return QueryMultiple(commandText, commandType, ReadSingleDefaultMapping<T>, null);
        }

        public List<T> QueryMultiple<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction) where T : new()
        {
            return QueryMultiple(commandText, commandType, mapFunction, null);
        }

        public List<T> QueryMultiple<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction, object? parameters) where T : new()
        {
            var com = CreateCommand(commandText, commandType, parameters);

            var items = new List<T>();
            using var results = com.ExecuteReader();
            while (results.Read())
            {
                T obj = ReadSingleDefaultMapping<T>(results);
                items.Add(obj);
            }

            return items;
        }

        public T QuerySingle<T>(string commandText, CommandType commandType, object? parameters) where T : new()
        {
            return QuerySingle(commandText, commandType, ReadSingleDefaultMapping<T>, parameters);
        }

        public T QuerySingle<T>(string commandText, CommandType commandType) where T : new()
        {
            return QuerySingle(commandText, commandType, ReadSingleDefaultMapping<T>, null);
        }

        public T QuerySingle<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction) where T : new()
        {
            return QuerySingle(commandText, commandType, mapFunction, null);
        }

        public T QuerySingle<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction, object? parameters) where T : new()
        {
            var com = CreateCommand(commandText, commandType, parameters);

            var result = new T();
            using var results =  com.ExecuteReader();
            if (results.Read())
            {
                result = mapFunction(results);
            }

            return result;
        }

        public void Close()
        {
             Connection.Close();
             Connection.Dispose();
        }
        #endregion Sync

        #region Helpers

        private static T ReadSingleDefaultMapping<T>(SqlDataReader reader) where T : new()
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
                if (!ShouldMapProperty(prop, out var name)) continue;

                if (!HasColumn(reader, name)) continue;

                var value = reader[name];
                if (value is DBNull) continue;

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

        private static bool TryConvert(object value, Type toType, out object? result)
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

        private static void AddParametersToCommand(SqlCommand command, object? parameters)
        {
            if (parameters is null) return;

            foreach (var prop in parameters.GetType().GetProperties())
            {
                if (!ShouldMapProperty(prop, out var mapName)) continue;

                var val = prop.GetValue(parameters);
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@{mapName}";
                parameter.Value = val ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

        private static bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool ShouldMapProperty(PropertyInfo prop, out string mapAs)
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

        private SqlCommand CreateCommand(string commandText, CommandType commandType, object? parameters)
        {
            ValidateConnection();
            var com = Connection.CreateCommand();
            com.CommandText = commandText;
            com.CommandType = commandType;
            AddParametersToCommand(com, parameters);

            return com;
        }
        #endregion Helpers
    }
}
