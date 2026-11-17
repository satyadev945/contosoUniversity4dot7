using ContosoUniversity.Models;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace ContosoUniversity.DAL
{
    public class SchoolContext : DbContext
    {
        // Connection string is supplied through the environment (e.g. ConnectionStrings__SchoolContext)
        // so the container image contains no environment-specific settings.
        private const string ConnectionStringEnvVar = "ConnectionStrings__SchoolContext";

        public SchoolContext() : base(ResolveConnectionString())
        {
        }

        private static string ResolveConnectionString()
        {
            var value = System.Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
            return string.IsNullOrWhiteSpace(value) ? "SchoolContext" : value;
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