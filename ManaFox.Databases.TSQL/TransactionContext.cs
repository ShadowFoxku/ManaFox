using Microsoft.Data.SqlClient;

namespace ManaFox.Databases.TSQL
{
    internal class TransactionContext(SqlConnection connection, SqlTransaction sqlTransaction)
    {
        public SqlConnection Connection { get; } = connection;
        public SqlTransaction SqlTransaction { get; } = sqlTransaction;
        public bool IsActive { get; set; } = true;
    }
}
