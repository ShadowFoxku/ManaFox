using ManaFox.Core.Flow;
using ManaFox.Databases.Core.Interfaces;
using System.Data;

namespace ManaFox.Databases.Core.Base
{
    public class RitualRuneReader(IRuneReader internalReader) : IRitualRuneReader
    {
        protected IRuneReader Internal = internalReader;
        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            await Internal.DisposeAsync();
        }

        public virtual Task CloseAsync() => Internal.CloseAsync();
        public virtual Task<Ritual<int>> ExecuteAsync(string commandText, CommandType commandType, object? parameters)
            => Ritual<int>.TryAsync(() => Internal.ExecuteAsync(commandText, commandType, parameters));
        public virtual Task<Ritual<List<T>>> QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new()
            => Ritual<List<T>>.TryAsync(() => Internal.QueryMultipleAsync(commandText, commandType, mapFunction, parameters));
        public virtual Task<Ritual<T>> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new()
            => Ritual<T>.TryAsync(() => Internal.QuerySingleAsync(commandText, commandType, mapFunction, parameters));
        public virtual Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new()
            => Ritual<T>.TryAsync(() => Internal.QuerySingleOrDefaultAsync(commandText, commandType, mapFunction, parameters));

        public virtual Task<Ritual<int>> ExecuteAsync(string commandText, CommandType commandType)
        {
            return ExecuteAsync(commandText, commandType, null);
        }

        public virtual Task<Ritual<List<T>>> QueryMultipleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new()
        {
            return QueryMultipleAsync(commandText, commandType, RuneReaderBase.ReadSingleDefaultMapping<T>, parameters);
        }

        public virtual Task<Ritual<List<T>>> QueryMultipleAsync<T>(string commandText, CommandType commandType) where T : new()
        {
            return QueryMultipleAsync(commandText, commandType, RuneReaderBase.ReadSingleDefaultMapping<T>, null);
        }

        public virtual Task<Ritual<List<T>>> QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new()
        {
            return QueryMultipleAsync(commandText, commandType, mapFunction, null);
        }

        public virtual Task<Ritual<T>> QuerySingleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new()
        {
            return QuerySingleAsync(commandText, commandType, RuneReaderBase.ReadSingleDefaultMapping<T>, parameters);
        }

        public virtual Task<Ritual<T>> QuerySingleAsync<T>(string commandText, CommandType commandType) where T : new()
        {
            return QuerySingleAsync(commandText, commandType, RuneReaderBase.ReadSingleDefaultMapping<T>, null);
        }

        public virtual Task<Ritual<T>> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new()
        {
            return QuerySingleAsync(commandText, commandType, mapFunction, null);
        }

        public virtual Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new()
        {
            return QuerySingleOrDefaultAsync(commandText, commandType, RuneReaderBase.ReadSingleDefaultMapping<T>, parameters);
        }

        public virtual Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType) where T : new()
        {
            return QuerySingleOrDefaultAsync(commandText, commandType, RuneReaderBase.ReadSingleDefaultMapping<T>, null);
        }

        public virtual Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new()
        {
            return QuerySingleOrDefaultAsync(commandText, commandType, mapFunction, null);
        }
    }
}
