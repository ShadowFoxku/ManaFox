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
        
        public async Task ApplySqlFolderAsync(string folderPath)
        {
            var sqlFiles = Directory
                .GetFiles(folderPath, "*.sql", SearchOption.AllDirectories)
                .OrderBy(f => f)
                .ToList();

            var allStatements = await SqlBatchExecutor.LoadStatementsAsync(sqlFiles);

            var tableStatements = new List<(string Source, string Sql)>();
            var deferredStatements = new List<(string Source, string Sql)>(); // table references

            foreach (var (source, sql) in allStatements)
            {
                if (TableSqlSplitter.LooksLikeCreateTable(sql))
                {
                    var (tableSql, constraints) = TableSqlSplitter.ExtractForeignKeys(sql);
                    tableStatements.Add((source, tableSql));
                    deferredStatements.AddRange(constraints.Select(c => (source, c)));
                }
                else
                {
                    deferredStatements.Add((source, sql));
                }
            }

            await SqlBatchExecutor.ExecuteWithDependencyRetryAsync(_connection, tableStatements);
            await SqlBatchExecutor.ExecuteWithDependencyRetryAsync(_connection, deferredStatements);
        }

        public async Task<DatabaseSchema> ReadSchemaAsync(MigratorOptions? options = null)
        {
            return await PostgresSchemaReader.ReadAsync(_connection, options);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
