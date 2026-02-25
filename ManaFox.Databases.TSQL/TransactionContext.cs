using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace ManaFox.Databases.TSQL
{
    internal class TransactionContext(SqlConnection connection, DbTransaction sqlTransaction)
    {
        public SqlConnection Connection { get; } = connection;
        public DbTransaction SqlTransaction { get; } = sqlTransaction;
        public bool IsActive { get; set; } = true;
    }
}
