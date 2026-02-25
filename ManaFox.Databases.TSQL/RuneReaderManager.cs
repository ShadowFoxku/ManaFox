using ManaFox.Databases.Core.Base;
using ManaFox.Databases.Core.Interfaces;
using Microsoft.Data.SqlClient;

namespace ManaFox.Databases.TSQL
{
    public class RuneReaderManager(IRuneReaderConfiguration config) : RuneReaderManagerBase(config), IRuneReaderManager
    {
        private const string DefaultKey = "Default"; // Note, this is only used for transactions
        private static readonly AsyncLocal<Dictionary<string, TransactionContext>> _transactionContexts = new();

        public override bool IsInTransaction => GetContexts().Values.Any(ctx => ctx.IsActive);

        private static Dictionary<string, TransactionContext> GetContexts()
        {
            _transactionContexts.Value ??= [];
            return _transactionContexts.Value;
        }

        private async Task<IRuneReader> CreateRuneReaderAsync(string? key = null, CancellationToken cancellationToken = default)
        {
            var contexts = GetContexts();
            if (contexts.TryGetValue(key ?? DefaultKey, out var context) && context.IsActive)
            {
                return new RuneReader(context.Connection, context.SqlTransaction);
            }

            var conn = new SqlConnection(GetConnectionString(key));
            await conn.OpenAsync(cancellationToken);
            return new RuneReader(conn);
        }

        public override Task<IRuneReader> GetRuneReaderAsync(CancellationToken cancellationToken = default)
            => CreateRuneReaderAsync(cancellationToken: cancellationToken);

        public override Task<IRuneReader> GetRuneReaderAsync(string key, CancellationToken cancellationToken = default)
            => CreateRuneReaderAsync(key, cancellationToken);

        public override Task BeginTransactionAsync(CancellationToken cancellationToken = default) 
            => BeginTransactionAsync(DefaultKey, cancellationToken);

        public override async Task BeginTransactionAsync(string key, CancellationToken cancellationToken = default)
        {
            var contexts = GetContexts();
            if (contexts.TryGetValue(key, out var existingContext) && existingContext.IsActive)
                throw new InvalidOperationException("A transaction is already active. Call CommitAsync or RollbackAsync first.");

            var conn = new SqlConnection(GetConnectionString(key));
            await conn.OpenAsync(cancellationToken);
            var sqlTransaction = await conn.BeginTransactionAsync(cancellationToken); 

            contexts[key] = new TransactionContext(conn, sqlTransaction);
        }

        public override Task CommitAsync(CancellationToken cancellationToken = default)
            => CommitAsync(DefaultKey, cancellationToken);

        public override async Task CommitAsync(string key, CancellationToken cancellationToken = default)
        {
            var contexts = GetContexts();
            if (!contexts.TryGetValue(key, out var context) || !context.IsActive)
                throw new InvalidOperationException("No active transaction to commit.");

            context.IsActive = false;
            try
            {
                await context.SqlTransaction.CommitAsync(cancellationToken);
            }
            catch
            {
                try { await context.SqlTransaction.RollbackAsync(CancellationToken.None); }
                catch { }
                throw;
            }
            finally
            {
                await CleanupContextAsync(key, context, contexts);
            }
        }

        public override Task RollbackAsync(CancellationToken cancellationToken = default)
            => RollbackAsync(DefaultKey, cancellationToken);

        public override async Task RollbackAsync(string key, CancellationToken cancellationToken = default)
        {
            var contexts = GetContexts();
            if (!contexts.TryGetValue(key, out var context) || !context.IsActive)
                throw new InvalidOperationException("No active transaction to rollback.");

            context.IsActive = false;
            try
            {
                await context.SqlTransaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await CleanupContextAsync(key, context, contexts);
            }
        }

        private static async Task CleanupContextAsync(string key, TransactionContext context, Dictionary<string, TransactionContext> contexts)
        {
            contexts.Remove(key); 
            try
            {
                await context.SqlTransaction.DisposeAsync();
                await context.Connection.CloseAsync();
                await context.Connection.DisposeAsync();
            }
            catch
            {
            }
        }
    }
}
