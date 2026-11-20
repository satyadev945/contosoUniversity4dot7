using System.Data.Entity;

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