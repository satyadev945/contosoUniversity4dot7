using System;
using System.Data.SqlClient;

namespace ContosoUniversity.DAL
{
    /// <summary>
    /// Resolves the database connection string from configuration / environment variables
    /// (no Web.config or LocalDB available when running in a Linux container).
    /// </summary>
    public static class ConnectionStringProvider
    {
        private static string _configured;

        public static void Configure(string connectionString)
        {
            _configured = connectionString;
        }

        public static string Get()
        {
            if (!string.IsNullOrWhiteSpace(_configured))
            {
                return _configured;
            }

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost",
                InitialCatalog = Environment.GetEnvironmentVariable("DB_NAME") ?? "ContosoUniversity",
                UserID = Environment.GetEnvironmentVariable("DB_USER") ?? string.Empty,
                Password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty,
                MultipleActiveResultSets = true
            };
            return builder.ConnectionString;
        }
    }
}
