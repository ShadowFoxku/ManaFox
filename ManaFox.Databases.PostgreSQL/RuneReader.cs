using ManaFox.Core.Errors;
using ManaFox.Databases.Core;
using ManaFox.Databases.Core.Base;
using ManaFox.Databases.Core.Interfaces;
using Npgsql;
using System.Data;
using System.Data.Common;

namespace ManaFox.Databases.PostgreSQL
{
    public class RuneReader(NpgsqlConnection conn, DbTransaction? transaction = null) : RuneReaderBase, IRuneReader
    {
        private readonly NpgsqlConnection Connection = conn;
        private readonly DbTransaction? Transaction = transaction;
        private readonly bool OwnsConnection = transaction is null;

        public override async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            if (Connection.State != ConnectionState.Closed && OwnsConnection)
                await CloseAsync();
        }

        ~RuneReader()
        {
            if (Connection.State != ConnectionState.Closed && OwnsConnection)
                _ = CloseAsync();
        }

        private void ValidateConnection()
        {
            if (Connection.State != ConnectionState.Open)
                throw new InvalidOperationException("The database connection is not open.");
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
            await using var results = RowReader.For(await com.ExecuteReaderAsync());
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

            await using var results = RowReader.For(await com.ExecuteReaderAsync());
            if (await results.ReadAsync())
                return mapFunction(results);

            throw new TearException("The requested query did not return any results.", new Tear("No results were found for the given query", DBErrorCodes.QueryReturnedNoResults));
        }

        public override async Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters)
        {
            var com = CreateCommand(commandText, commandType, parameters);

            await using var results = RowReader.For(await com.ExecuteReaderAsync());
            if (await results.ReadAsync())
                return mapFunction(results);

#pragma warning disable CS8603
            return default;
#pragma warning restore CS8603
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

        private static void AddParametersToCommand(NpgsqlCommand command, object? parameters)
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

        private NpgsqlCommand CreateCommand(string commandText, CommandType commandType, object? parameters)
        {
            ValidateConnection();
            var com = Connection.CreateCommand();
            com.CommandText = commandText;
            com.CommandType = commandType;

            if (Transaction != null)
                com.Transaction = (NpgsqlTransaction)Transaction;

            AddParametersToCommand(com, parameters);

            return com;
        }

        #endregion Helpers
    }
}
