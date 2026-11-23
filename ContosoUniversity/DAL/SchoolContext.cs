using ContosoUniversity.Models;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace ContosoUniversity.DAL
{
    public class SchoolContext : DbContext
    {
        public SchoolContext() : base(GetConnectionString())
        {
        }

        // Connection string is supplied through the environment (no machine-specific LocalDB).
        // Set ConnectionStrings__SchoolContext, or DB_HOST/DB_NAME/DB_USER/DB_PASSWORD.
        private static string GetConnectionString()
        {
            var full = System.Environment.GetEnvironmentVariable("ConnectionStrings__SchoolContext");
            if (!string.IsNullOrEmpty(full))
            {
                return full;
            }
            var host = System.Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
            var name = System.Environment.GetEnvironmentVariable("DB_NAME") ?? "ContosoUniversity";
            var user = System.Environment.GetEnvironmentVariable("DB_USER") ?? "sa";
            var password = System.Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty;
            return string.Format("Server={0};Database={1};User Id={2};Password={3};TrustServerCertificate=True", host, name, user, password);
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Instructor> Instructors { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<OfficeAssignment> OfficeAssignments { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<Course>()
                .HasMany(c => c.Instructors).WithMany(i => i.Courses)
                .Map(t => t.MapLeftKey("CourseID")
                    .MapRightKey("InstructorID")
                    .ToTable("CourseInstructor"));
        }
    }
}