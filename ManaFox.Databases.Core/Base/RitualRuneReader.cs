using ManaFox.Core.Flow;
using ManaFox.Databases.Core.Interfaces;
using System.Data;
using ManaFox.Databases.Core.QueryBuilders;

namespace ManaFox.Databases.Core.Base
{
    public class RitualRuneReader(IRuneReader internalReader) : IRitualRuneReader
    {
        private readonly IRuneReader _internal = internalReader;
        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            await _internal.DisposeAsync();
        }
        public Task CloseAsync() => _internal.CloseAsync();

        #region  Execute
        public Task<Ritual<int>> ExecuteAsync(string commandText, CommandType commandType, object? parameters)
            => Ritual<int>.TryAsync(() => _internal.ExecuteAsync(commandText, commandType, parameters));
        
        public Task<Ritual<int>> ExecuteAsync(string commandText, CommandType commandType)
            => ExecuteAsync(commandText, commandType, null);

        public Task<Ritual<int>> ExecuteAsync<T>(SearchQueryBuilder<T> query)
            => ExecuteAsync(query.Build(), CommandType.Text, query.Parameters);

        #endregion Execute

        #region Single
        public Task<Ritual<T>> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new()
            => Ritual<T>.TryAsync(() => _internal.QuerySingleAsync(commandText, commandType, mapFunction, parameters));
        public Task<Ritual<T>> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new()
            => QuerySingleAsync(commandText, commandType, mapFunction, null);
        
        public Task<Ritual<T>> QuerySingleAsync<T>(SearchQueryBuilder<T> query, Func<IRowReader, T> mapFunction) where T : new()
            => QuerySingleAsync(query.Build(), CommandType.Text, mapFunction, null);

        public Task<Ritual<T>> QuerySingleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new()
            => QuerySingleAsync(commandText, commandType, RuneReaderBase.ReadSingleDefaultMapping<T>, parameters);

        public Task<Ritual<T>> QuerySingleAsync<T>(string commandText, CommandType commandType) where T : new()
            => QuerySingleAsync(commandText, commandType, RuneReaderBase.ReadSingleDefaultMapping<T>, null);

        public Task<Ritual<T>> QuerySingleAsync<T>(SearchQueryBuilder<T> query) where T : new()
            => QuerySingleAsync<T>(query.Build(), CommandType.Text, query.Parameters);
        
        #endregion Single

        #region  Single or Default
        public Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new()
            => Ritual<T>.TryAsync(() => _internal.QuerySingleOrDefaultAsync(commandText, commandType, mapFunction, parameters));
        
        public Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new()
            => QuerySingleOrDefaultAsync(commandText, commandType, RuneReaderBase.ReadSingleDefaultMapping<T>, parameters);
        
        public Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new()
            => QuerySingleOrDefaultAsync(commandText, commandType, mapFunction, null);

        public Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType) where T : new()
            => QuerySingleOrDefaultAsync(commandText, commandType, RuneReaderBase.ReadSingleDefaultMapping<T>, null);

        public Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(SearchQueryBuilder<T> query, Func<IRowReader, T> mapFunction) where T : new()
            => QuerySingleOrDefaultAsync(query.Build(), CommandType.Text, mapFunction, query.Parameters);
        
        public Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(SearchQueryBuilder<T> query) where T : new()
            => QuerySingleOrDefaultAsync<T>(query.Build(), CommandType.Text, query.Parameters);
        #endregion

        #region  Multiple        
        public Task<Ritual<List<T>>> QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new()
            => Ritual<List<T>>.TryAsync(() => _internal.QueryMultipleAsync(commandText, commandType, mapFunction, parameters));
        
        public Task<Ritual<List<T>>> QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new()
            => QueryMultipleAsync(commandText, commandType, mapFunction, null);

        public Task<Ritual<List<T>>> QueryMultipleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new()
            => QueryMultipleAsync(commandText, commandType, RuneReaderBase.ReadSingleDefaultMapping<T>, parameters);

        public Task<Ritual<List<T>>> QueryMultipleAsync<T>(string commandText, CommandType commandType) where T : new()
            => QueryMultipleAsync(commandText, commandType, RuneReaderBase.ReadSingleDefaultMapping<T>, null);

        public Task<Ritual<List<T>>> QueryMultipleAsync<T>(SearchQueryBuilder<T> query) where T : new()
            => QueryMultipleAsync<T>(query.Build(), CommandType.Text, query.Parameters);
        
        public Task<Ritual<List<T>>> QueryMultipleAsync<T>(SearchQueryBuilder<T> query, Func<IRowReader, T> mapFunction) where T : new()
            => QueryMultipleAsync(query.Build(), CommandType.Text, mapFunction, query.Parameters);
        #endregion
    }
}
