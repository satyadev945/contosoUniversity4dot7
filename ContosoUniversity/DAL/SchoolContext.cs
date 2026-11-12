using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.DAL
{
    public class SchoolContext : DbContext
    {
        public SchoolContext(DbContextOptions<SchoolContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Instructor> Instructors { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<OfficeAssignment> OfficeAssignments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Singular table names (equivalent of removing PluralizingTableNameConvention)
            modelBuilder.Entity<Course>().ToTable("Course");
            modelBuilder.Entity<Department>().ToTable("Department");
            modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
            modelBuilder.Entity<Instructor>().ToTable("Instructor");
            modelBuilder.Entity<Student>().ToTable("Student");
            modelBuilder.Entity<OfficeAssignment>().ToTable("OfficeAssignment");

            modelBuilder.Entity<Student>().Property(s => s.FirstMidName).HasColumnName("FirstName");
            modelBuilder.Entity<Instructor>().Property(i => i.FirstMidName).HasColumnName("FirstName");

            modelBuilder.Entity<Course>()
                .HasMany(c => c.Instructors).WithMany(i => i.Courses)
                .UsingEntity<System.Collections.Generic.Dictionary<string, object>>(
                    "CourseInstructor",
                    r => r.HasOne<Instructor>().WithMany().HasForeignKey("InstructorID"),
                    l => l.HasOne<Course>().WithMany().HasForeignKey("CourseID"));
        }
    }
}
