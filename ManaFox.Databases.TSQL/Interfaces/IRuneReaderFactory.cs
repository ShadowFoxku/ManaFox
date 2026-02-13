namespace ManaFox.Databases.TSQL.Interfaces
{
    public interface IRuneReaderFactory
    {
        Task<IRuneReader> GetRuneReaderAsync(CancellationToken cancellationToken = default);
        Task<IRuneReader> GetRuneReaderAsync(string key, CancellationToken cancellationToken = default);

        IRuneReader GetRuneReader();
        IRuneReader GetRuneReader(string key);
    }
}
