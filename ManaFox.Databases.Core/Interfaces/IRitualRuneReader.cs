using ManaFox.Core.Flow;
using System.Data;
using ManaFox.Databases.Core.QueryBuilders;

namespace ManaFox.Databases.Core.Interfaces
{
    public interface IRitualRuneReader : IAsyncDisposable
    {
        Task<Ritual<int>> ExecuteAsync(string commandText, CommandType commandType, object? parameters);
        Task<Ritual<int>> ExecuteAsync(string commandText, CommandType commandType);
        Task<Ritual<int>> ExecuteAsync<T>(SearchQueryBuilder<T> query);

        Task<Ritual<List<T>>>  QueryMultipleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new();
        Task<Ritual<List<T>>>  QueryMultipleAsync<T>(string commandText, CommandType commandType) where T : new();
        Task<Ritual<List<T>>>  QueryMultipleAsync<T>(SearchQueryBuilder<T> query) where T : new();
        Task<Ritual<List<T>>>  QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new();
        Task<Ritual<List<T>>>  QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new();
        Task<Ritual<List<T>>>  QueryMultipleAsync<T>(SearchQueryBuilder<T> query, Func<IRowReader, T> mapFunction) where T : new();

        Task<Ritual<T>> QuerySingleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new();
        Task<Ritual<T>> QuerySingleAsync<T>(string commandText, CommandType commandType) where T : new();
        Task<Ritual<T>> QuerySingleAsync<T>(SearchQueryBuilder<T> query) where T : new();
        Task<Ritual<T>> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new();
        Task<Ritual<T>> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new();
        Task<Ritual<T>> QuerySingleAsync<T>(SearchQueryBuilder<T> query, Func<IRowReader, T> mapFunction) where T : new();

        Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new();
        Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType) where T : new();
        Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(SearchQueryBuilder<T> query) where T : new();
        Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new();
        Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new();
        Task<Ritual<T>> QuerySingleOrDefaultAsync<T>(SearchQueryBuilder<T> query, Func<IRowReader, T> mapFunction) where T : new();

        Task CloseAsync();
    }
}
