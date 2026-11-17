using System.Data.Entity;
using System.Data.Entity.Infrastructure;

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