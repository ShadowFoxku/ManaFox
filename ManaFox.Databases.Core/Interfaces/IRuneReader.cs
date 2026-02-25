using System.Data;

namespace ManaFox.Databases.Core.Interfaces
{
    public interface IRuneReader : IAsyncDisposable
    {
        Task<int> ExecuteAsync(string commandText, CommandType commandType, object? parameters);
        Task<int> ExecuteAsync(string commandText, CommandType commandType);

        Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new();
        Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType) where T : new();
        Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new();
        Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new();

        Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new();
        Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType) where T : new();
        Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new();
        Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new();

        Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new();
        Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType) where T : new();
        Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction, object? parameters) where T : new();
        Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, Func<IRowReader, T> mapFunction) where T : new();

        Task CloseAsync();
    }
}
