using ContosoUniversity.Models;
using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace ContosoUniversity.DAL
{
    public class SchoolContext : DbContext
    {
        // Connection string is supplied through the environment (e.g. a Kubernetes Secret/ConfigMap).
        private static string ResolveConnectionString()
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SchoolContext");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = Environment.GetEnvironmentVariable("SCHOOL_DB_CONNECTION_STRING");
            }
            return string.IsNullOrWhiteSpace(connectionString) ? "ContosoUniversity.DAL.SchoolContext" : connectionString;
        }

        public SchoolContext() : base(ResolveConnectionString())
        {
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