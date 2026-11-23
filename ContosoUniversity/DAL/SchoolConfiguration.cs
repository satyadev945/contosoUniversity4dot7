using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;

namespace ContosoUniversity.DAL
{
    public class SchoolConfiguration : DbConfiguration
    {
        public SchoolConfiguration()
        {
            // No Web.config on ASP.NET Core: register the SQL Server provider in code
            SetProviderFactory("System.Data.SqlClient", SqlClientFactory.Instance);
            SetProviderServices("System.Data.SqlClient", SqlProviderServices.Instance);
            SetDefaultConnectionFactory(new SqlConnectionFactory());
            SetExecutionStrategy("System.Data.SqlClient", () => new SqlAzureExecutionStrategy());
        }
    }
}
