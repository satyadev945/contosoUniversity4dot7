using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Data
{
    public class SchoolContext : DbContext
    {
        public SchoolContext(DbContextOptions<SchoolContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Enrollment> Enrollments { get; set; } = null!;
        public DbSet<Instructor> Instructors { get; set; } = null!;
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<OfficeAssignment> OfficeAssignments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Preserve the singular table names used by the EF6 model (PluralizingTableNameConvention was removed).
            modelBuilder.Entity<Course>().ToTable("Course");
            modelBuilder.Entity<Department>().ToTable("Department");
            modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
            modelBuilder.Entity<Instructor>().ToTable("Instructor");
            modelBuilder.Entity<Student>().ToTable("Student");
            modelBuilder.Entity<OfficeAssignment>().ToTable("OfficeAssignment");

            // Many-to-many Course <-> Instructor using the existing CourseInstructor join table.
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

            // Department.InstructorID is optional: do not cascade-delete departments with their administrator.
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Administrator)
                .WithMany()
                .HasForeignKey(d => d.InstructorID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<OfficeAssignment>()
                .HasOne(o => o.Instructor)
                .WithOne(i => i.OfficeAssignment)
                .HasForeignKey<OfficeAssignment>(o => o.InstructorID);
        }
    }
}
