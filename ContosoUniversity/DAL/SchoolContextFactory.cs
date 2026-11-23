using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContosoUniversity.DAL
{
    /// <summary>
    /// Design-time factory used by the EF Core tools (dotnet ef migrations ...).
    /// </summary>
    public class SchoolContextFactory : IDesignTimeDbContextFactory<SchoolContext>
    {
        public SchoolContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SchoolContext>();
            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=ContosoUniversity.DAL.SchoolContext;Trusted_Connection=True;TrustServerCertificate=True");
            return new SchoolContext(optionsBuilder.Options);
        }
    }
}
