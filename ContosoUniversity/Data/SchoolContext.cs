using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Data
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
            base.OnModelCreating(modelBuilder);

            // Keep the singular table names used by the original EF6 model
            // (PluralizingTableNameConvention was removed there).
            modelBuilder.Entity<Course>().ToTable("Course");
            modelBuilder.Entity<Department>().ToTable("Department");
            modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
            modelBuilder.Entity<Instructor>().ToTable("Instructor");
            modelBuilder.Entity<Student>().ToTable("Student");
            modelBuilder.Entity<OfficeAssignment>().ToTable("OfficeAssignment");

            // EF6 mapped DateTime to "datetime"; keep this to avoid schema drift.
            modelBuilder.Entity<Student>().Property(s => s.EnrollmentDate).HasColumnType("datetime");
            modelBuilder.Entity<Instructor>().Property(i => i.HireDate).HasColumnType("datetime");
            modelBuilder.Entity<Department>().Property(d => d.StartDate).HasColumnType("datetime");

            // Many-to-many Course <-> Instructor with the existing join table and column names.
            modelBuilder.Entity<Course>()
                .HasMany(c => c.Instructors)
                .WithMany(i => i.Courses)
                .UsingEntity<Dictionary<string, object>>(
                    "CourseInstructor",
                    right => right.HasOne<Instructor>().WithMany().HasForeignKey("InstructorID").OnDelete(DeleteBehavior.Cascade),
                    left => left.HasOne<Course>().WithMany().HasForeignKey("CourseID").OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.ToTable("CourseInstructor");
                        join.HasKey("CourseID", "InstructorID");
                    });
        }
    }
}
