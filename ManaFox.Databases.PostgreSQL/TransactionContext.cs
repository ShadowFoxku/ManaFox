using Npgsql;
using System.Data.Common;

namespace ManaFox.Databases.PostgreSQL
{
    internal class TransactionContext(NpgsqlConnection connection, DbTransaction transaction)
    {
        public NpgsqlConnection Connection { get; } = connection;
        public DbTransaction Transaction { get; } = transaction;
        public bool IsActive { get; set; } = true;
    }
}
