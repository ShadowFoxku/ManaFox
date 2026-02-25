using ManaFox.Core.Flow;
using ManaFox.Databases.Core.Interfaces;

namespace ManaFox.Databases.Core.Base
{
    public abstract class RuneReaderManagerBase : IRuneReaderManager
    {
        public abstract Task<IRuneReader> GetRuneReaderAsync(CancellationToken cancellationToken = default);
        public virtual async Task<IRitualRuneReader> GetRitualRuneReaderAsync(CancellationToken cancellationToken = default)
        {
            return new RitualRuneReader(await GetRuneReaderAsync(cancellationToken));
        }

        public abstract Task<IRuneReader> GetRuneReaderAsync(string key, CancellationToken cancellationToken = default);
        public virtual async Task<IRitualRuneReader> GetRitualRuneReaderAsync(string key, CancellationToken cancellationToken = default)
        {
            return new RitualRuneReader(await GetRuneReaderAsync(key, cancellationToken));
        }

        public abstract Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        public abstract Task BeginTransactionAsync(string key, CancellationToken cancellationToken = default);

        public abstract Task CommitAsync(CancellationToken cancellationToken = default);
        public abstract Task CommitAsync(string key, CancellationToken cancellationToken = default);

        public abstract Task RollbackAsync(CancellationToken cancellationToken = default);
        public abstract Task RollbackAsync(string key, CancellationToken cancellationToken = default);

        public async Task<Ritual<T>> RunInTransactionAsync<T>(string database, Func<Task<Ritual<T>>> operation)
        {
            await BeginTransactionAsync(database);
            try
            {
                var result = await operation();

                if (result.IsFlowing)
                    await CommitAsync(database);
                else
                    await RollbackAsync(database);

                return result;
            }
            catch
            {
                try { await RollbackAsync(database); } catch { }
                throw;
            }
        }

        public async Task<Ritual<T>> TryRunInTransactionAsync<T>(string database, Func<Task<T>> operation)
        {
            return await Ritual<T>.TryAsync(async () =>
            {
                await BeginTransactionAsync(database);
                try
                {
                    var result = await operation();
                    await CommitAsync(database);
                    return result;
                }
                catch
                {
                    try { await RollbackAsync(database); } catch { }
                    throw;
                }
            });
        }

        public abstract bool IsInTransaction { get; }

        protected readonly IRuneReaderConfiguration Configuration;

        protected RuneReaderManagerBase(IRuneReaderConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config);
            Configuration = config;
        }

        protected string GetConnectionString(string? key = null)
        {
            string? connString;
            if (key == null)
                Configuration.TryGetDefaultString(out connString);
            else
                Configuration.TryGetString(key, out connString);

            if (string.IsNullOrWhiteSpace(connString))
                throw new ArgumentException("The connection string for the given key was not found, or not valid", nameof(key));

            return connString;
        }
    }
}
