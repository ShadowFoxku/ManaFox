namespace ManaFox.Databases.Core.Interfaces
{
    public interface IRuneReaderFactory
    {
        Task<IRuneReader> GetRuneReaderAsync(CancellationToken cancellationToken = default);
        Task<IRuneReader> GetRuneReaderAsync(string key, CancellationToken cancellationToken = default);
    }
}
