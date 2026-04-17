using Microsoft.Data.SqlClient;

namespace ManaFox.Databases.TSQL.Migrations.embedded
{
    internal static class EnsureTvpTypesExist
    {
        public static void EnsureTvpTypes(string connectionString)
        {
            var assembly = typeof(EnsureTvpTypesExist).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .Single(n => n.EndsWith("TvpTableTypes.sql"));

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}
