using Npgsql;
using Testcontainers.PostgreSql;

namespace ManaFox.Databases.PostgreSQL.Migrations
{
    /// <summary>
    /// A short-lived PostgreSQL container that represents the *desired* state.
    /// SQL definition files are applied to it, then its schema is read and
    /// diffed against the live target.
    /// </summary>
    internal sealed class ShadowDatabase : IAsyncDisposable
    {
        private readonly PostgreSqlContainer _container;
        private readonly NpgsqlConnection _connection;

        private ShadowDatabase(PostgreSqlContainer container, NpgsqlConnection connection)
        {
            _container = container;
            _connection = connection;
        }

        public static async Task<ShadowDatabase> CreateAsync()
        {
            var container = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("shadow")
                .WithUsername("shadow")
                .WithPassword("shadow")
                .Build();

            await container.StartAsync();

            var conn = new NpgsqlConnection(container.GetConnectionString());
            await conn.OpenAsync();

            return new ShadowDatabase(container, conn);
        }

        /// <summary>
        /// Applies all .sql files in the folder to the shadow DB in alphabetical order.
        /// Naming your files with a numeric prefix (e.g. 01_tables.sql, 02_indexes.sql)
        /// gives you deterministic ordering.
        /// </summary>
        public async Task ApplySqlFolderAsync(string folderPath)
        {
            var sqlFiles = Directory
                .GetFiles(folderPath, "*.sql", SearchOption.AllDirectories)
                .OrderBy(f => f)
                .ToList();

            foreach (var file in sqlFiles)
            {
                var sql = await File.ReadAllTextAsync(file);
                if (string.IsNullOrWhiteSpace(sql)) continue;

                await using var cmd = _connection.CreateCommand();
                cmd.CommandText = sql;

                try
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to apply shadow schema from '{Path.GetFileName(file)}': {ex.Message}", ex);
                }
            }
        }

        public async Task<DatabaseSchema> ReadSchemaAsync()
        {
            return await PostgresSchemaReader.ReadAsync(_connection);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
