using ContosoUniversity.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace ContosoUniversity.DAL
{
    public class SchoolContext : DbContext
    {
        public SchoolContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }

        /// <summary>
        /// Resolves the SQL Server connection string from the environment
        /// (ConnectionStrings__SchoolContext / SCHOOL_DB_CONNECTION) or configuration.
        /// </summary>
        public static string ResolveConnectionString(IConfiguration configuration)
        {
            var connectionString = Environment.GetEnvironmentVariable("SCHOOL_DB_CONNECTION")
                ?? configuration?.GetConnectionString("SchoolContext");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Database connection string not configured. Set the SCHOOL_DB_CONNECTION or ConnectionStrings__SchoolContext environment variable.");
            }
            return connectionString;
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