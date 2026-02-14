using ManaFox.Databases.Core.Interfaces;

namespace ManaFox.Databases.Core.Base
{
    public abstract class RuneReaderFactoryBase : IRuneReaderFactory
    {
        public abstract Task<IRuneReader> GetRuneReaderAsync(CancellationToken cancellationToken = default);
        public abstract Task<IRuneReader> GetRuneReaderAsync(string key, CancellationToken cancellationToken = default);

        protected readonly IRuneReaderConfiguration _configuration;
        protected RuneReaderFactoryBase(IRuneReaderConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config);
            _configuration = config;
        }

        protected string GetConnectionString(string? key = null)
        {
            string? connString;
            if (key == null)
                _configuration.TryGetDefaultString(out connString);
            else
                _configuration.TryGetString(key, out connString);

            if (string.IsNullOrWhiteSpace(connString))
                throw new ArgumentException("The connection string for the given key was not found, or not valid", nameof(key));

            return connString;
        }
    }
}
