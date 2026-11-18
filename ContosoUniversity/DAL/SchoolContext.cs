using ContosoUniversity.Models;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System;

namespace ContosoUniversity.DAL
{
    public class SchoolContext : DbContext
    {
        // Connection string is supplied through the environment (SCHOOL_DB_CONNECTION or the
        // standard ASP.NET Core ConnectionStrings__SchoolContext variable) so the container
        // does not depend on Windows-only LocalDB.
        public SchoolContext() : base(ResolveConnectionString())
        {
        }

        private static string ResolveConnectionString()
        {
            return Environment.GetEnvironmentVariable("SCHOOL_DB_CONNECTION")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__SchoolContext")
                ?? "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=ContosoUniversity.DAL.SchoolContext;Integrated Security=True";
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