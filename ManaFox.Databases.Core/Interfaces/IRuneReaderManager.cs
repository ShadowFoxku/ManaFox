namespace ManaFox.Databases.Core.Interfaces
{
    public interface IRuneReaderManager
    {
        Task<IRuneReader> GetRuneReaderAsync(CancellationToken cancellationToken = default);

        Task<IRuneReader> GetRuneReaderAsync(string key, CancellationToken cancellationToken = default);

        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        Task BeginTransactionAsync(string key, CancellationToken cancellationToken = default);

        Task CommitAsync(CancellationToken cancellationToken = default);

        Task CommitAsync(string key, CancellationToken cancellationToken = default);

        Task RollbackAsync(CancellationToken cancellationToken = default);

        Task RollbackAsync(string key, CancellationToken cancellationToken = default);

        bool IsInTransaction { get; }
    }
}

