using ManaFox.Databases.Core.Base;
using ManaFox.Databases.Core.Interfaces;
using Microsoft.Data.SqlClient;

namespace ManaFox.Databases.TSQL
{
    public class RuneReaderFactory(IRuneReaderConfiguration config) : RuneReaderFactoryBase(config), IRuneReaderFactory
    {
        private async Task<IRuneReader> CreateRuneReaderAsync(string? key = null, CancellationToken cancellationToken = default)
        {
            var conn = new SqlConnection(GetConnectionString(key));
            await conn.OpenAsync(cancellationToken);
            return new RuneReader(conn);
        }

        public override Task<IRuneReader> GetRuneReaderAsync(CancellationToken cancellationToken = default)
            => CreateRuneReaderAsync(cancellationToken: cancellationToken);

        public override Task<IRuneReader> GetRuneReaderAsync(string key, CancellationToken cancellationToken = default)
            => CreateRuneReaderAsync(key, cancellationToken);
    }
}
