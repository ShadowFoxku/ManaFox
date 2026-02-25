using ManaFox.Core.Errors;
using ManaFox.Databases.Core;
using ManaFox.Databases.Core.Base;
using ManaFox.Databases.Core.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace ManaFox.Databases.TSQL
{
    public class RuneReader(SqlConnection conn, DbTransaction? transaction = null) : RuneReaderBase, IRuneReader
    {
        private readonly SqlConnection Connection = conn;
        private readonly DbTransaction? Transaction = transaction;
        private readonly bool OwnsConnection = transaction is null;

        public override async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            if (Connection.State != ConnectionState.Closed && OwnsConnection)
                await CloseAsync();
        }

        private void ValidateConnection()
        {
            if (Connection.State != ConnectionState.Open)
                throw new InvalidOperationException("The database connection is not open.");
        }

        ~RuneReader()
        {
            if (Connection.State != ConnectionState.Closed && OwnsConnection)
                _ = CloseAsync();
        }

        public override Task<int> ExecuteAsync(string commandText, CommandType commandType, object? parameters)
        {
            var com = CreateCommand(commandText, commandType, parameters);
            return com.ExecuteNonQueryAsync();
        }

        public override async Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) 
        {
            var com = CreateCommand(commandText, commandType, parameters);

            var items = new List<T>();
            using var results = RowReader.For(await com.ExecuteReaderAsync());
            while (await results.ReadAsync())
            {
                T obj = mapFunction(results);
                items.Add(obj);
            }

            return items;
        }

        public override async Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) 
        {
            var com = CreateCommand(commandText, commandType, parameters);

            using var results = RowReader.For(await com.ExecuteReaderAsync());
            if (await results.ReadAsync())
            {
                return mapFunction(results);
            }

            throw new TearException("The requested query did not return any results.", new Tear("No results were found for the given query", DBErrorCodes.QueryReturnedNoResults));
        }

        public override async Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters)
        {
            var com = CreateCommand(commandText, commandType, parameters);

            using var results = RowReader.For(await com.ExecuteReaderAsync());
            if (await results.ReadAsync())
            {
                return mapFunction(results);
            }

#pragma warning disable CS8603 // Possible null reference return.
            return default;
#pragma warning restore CS8603 // Possible null reference return.
        }

        public override async Task CloseAsync()
        {
            if (OwnsConnection)
            {
                await Connection.CloseAsync();
                await Connection.DisposeAsync();
            }
        }

        #region Helpers

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

        private SqlCommand CreateCommand(string commandText, CommandType commandType, object? parameters)
        {
            ValidateConnection();
            var com = Connection.CreateCommand();
            com.CommandText = commandText;
            com.CommandType = commandType;

            if (Transaction != null)
                com.Transaction = (SqlTransaction)Transaction;

            AddParametersToCommand(com, parameters);

            return com;
        }
        #endregion Helpers
    }
}
