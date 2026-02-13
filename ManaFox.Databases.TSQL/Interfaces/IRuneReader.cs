using Microsoft.Data.SqlClient;
using System.Data;

namespace ManaFox.Databases.TSQL.Interfaces
{
    public interface IRuneReader
    {
        #region Async
        Task<int> ExecuteAsync(string commandText, CommandType commandType, object? parameters);
        Task<int> ExecuteAsync(string commandText, CommandType commandType);

        Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new();
        Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType) where T : new();
        Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction, object? parameters) where T : new();
        Task<List<T>> QueryMultipleAsync<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction) where T : new();

        Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, object? parameters) where T : new();
        Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType) where T : new();
        Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction, object? parameters) where T : new();
        Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction) where T : new();

        Task CloseAsync();
        #endregion Async

        #region Sync
        int Execute(string commandText, CommandType commandType, object? parameters);
        int Execute(string commandText, CommandType commandType);

        List<T> QueryMultiple<T>(string commandText, CommandType commandType, object? parameters) where T : new();
        List<T> QueryMultiple<T>(string commandText, CommandType commandType) where T : new();
        List<T> QueryMultiple<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction, object? parameters) where T : new();
        List<T> QueryMultiple<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction) where T : new();

        T QuerySingle<T>(string commandText, CommandType commandType, object? parameters) where T : new();
        T QuerySingle<T>(string commandText, CommandType commandType) where T : new();
        T QuerySingle<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction, object? parameters) where T : new();
        T QuerySingle<T>(string commandText, CommandType commandType, Func<SqlDataReader, T> mapFunction) where T : new();

        void Close();
        #endregion Sync
    }
}
