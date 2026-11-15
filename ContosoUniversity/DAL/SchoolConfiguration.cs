using System.Data.Entity;
using Npgsql;

namespace ContosoUniversity.DAL
{
    public class SchoolConfiguration : DbConfiguration
    {
        public SchoolConfiguration()
        {
            SetExecutionStrategy("Npgsql", () => new DefaultExecutionStrategy());
        }
    }
}