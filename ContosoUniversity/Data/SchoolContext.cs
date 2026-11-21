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
            // The EF6 model used singular table names (PluralizingTableNameConvention was removed).
            modelBuilder.Entity<Course>().ToTable("Course");
            modelBuilder.Entity<Department>().ToTable("Department");
            modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
            modelBuilder.Entity<Instructor>().ToTable("Instructor");
            modelBuilder.Entity<Student>().ToTable("Student");
            modelBuilder.Entity<OfficeAssignment>().ToTable("OfficeAssignment");

            modelBuilder.Entity<Department>()
                .HasOne(d => d.Administrator)
                .WithMany()
                .HasForeignKey(d => d.InstructorID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<Instructor>()
                .HasOne(i => i.OfficeAssignment)
                .WithOne(o => o.Instructor)
                .HasForeignKey<OfficeAssignment>(o => o.InstructorID);

            // Many-to-many join table, matching the EF6 mapping (CourseInstructor: CourseID, InstructorID).
            modelBuilder.Entity<Course>()
                .HasMany(c => c.Instructors)
                .WithMany(i => i.Courses)
                .UsingEntity<Dictionary<string, object>>(
                    "CourseInstructor",
                    r => r.HasOne<Instructor>().WithMany().HasForeignKey("InstructorID").OnDelete(DeleteBehavior.Cascade),
                    l => l.HasOne<Course>().WithMany().HasForeignKey("CourseID").OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.ToTable("CourseInstructor");
                        j.HasKey("CourseID", "InstructorID");
                    });
        }
    }
}
