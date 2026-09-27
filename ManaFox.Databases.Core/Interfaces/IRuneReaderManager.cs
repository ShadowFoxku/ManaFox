using ManaFox.Core.Flow;

namespace ManaFox.Databases.Core.Interfaces
{
    public interface IRuneReaderManager
    {
        Task<IRuneReader> GetRuneReaderAsync(CancellationToken cancellationToken = default);
        Task<IRitualRuneReader> GetRitualRuneReaderAsync(CancellationToken cancellationToken = default);

        Task<IRuneReader> GetRuneReaderAsync(string key, CancellationToken cancellationToken = default);
        Task<IRitualRuneReader> GetRitualRuneReaderAsync(string key, CancellationToken cancellationToken = default);


        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(string txnKey, CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(string txnKey, string dbKey, CancellationToken cancellationToken = default);

        Task CommitAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(string txnKey, CancellationToken cancellationToken = default);

        Task RollbackAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(string txnKey, CancellationToken cancellationToken = default);

        Task<Ritual<T>> RunInTransactionAsync<T>(string database, Func<Task<Ritual<T>>> operation);
        Task<Ritual<T>> TryRunInTransactionAsync<T>(string database, Func<Task<T>> operation);

        bool IsInTransaction { get; }
    }
}

