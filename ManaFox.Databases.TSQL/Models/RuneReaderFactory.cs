using ManaFox.Databases.TSQL.Interfaces;
using Microsoft.Data.SqlClient;

namespace ManaFox.Databases.TSQL.Models
{
    public class RuneReaderFactory : IRuneReaderFactory
    {
        private readonly IRuneReaderConfiguration _configuration;
        public RuneReaderFactory(IRuneReaderConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config);
            _configuration = config;
        }

        private string GetConnectionString(string? key = null)
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

        private SqlConnection GetConnection(string? key = null)
        {
            return new SqlConnection(GetConnectionString(key));
        }

        private RuneReader CreateRuneReader(string? key = null)
        {
            var conn = GetConnection(key);
            conn.Open();
            return new RuneReader(conn);
        }

        private async Task<IRuneReader> CreateRuneReaderAsync(string? key = null, CancellationToken cancellationToken = default)
        {
            var conn = GetConnection(key);
            await conn.OpenAsync(cancellationToken);
            return new RuneReader(conn);
        }

        public IRuneReader GetRuneReader() => CreateRuneReader();

        public IRuneReader GetRuneReader(string key) => CreateRuneReader(key);

        public Task<IRuneReader> GetRuneReaderAsync(CancellationToken cancellationToken = default)
            => CreateRuneReaderAsync(cancellationToken: cancellationToken);

        public Task<IRuneReader> GetRuneReaderAsync(string key, CancellationToken cancellationToken = default)
            => CreateRuneReaderAsync(key, cancellationToken);
    }
}
