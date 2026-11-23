using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.DAL
{
    public class SchoolContext : DbContext
    {
        public SchoolContext(DbContextOptions<SchoolContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<Instructor> Instructors => Set<Instructor>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<OfficeAssignment> OfficeAssignments => Set<OfficeAssignment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // The legacy EF6 model removed the pluralizing convention, so tables use singular names.
            modelBuilder.Entity<Course>().ToTable("Course");
            modelBuilder.Entity<Department>().ToTable("Department");
            modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
            modelBuilder.Entity<Instructor>().ToTable("Instructor");
            modelBuilder.Entity<Student>().ToTable("Student");
            modelBuilder.Entity<OfficeAssignment>().ToTable("OfficeAssignment");

            modelBuilder.Entity<Course>()
                .HasMany(c => c.Instructors)
                .WithMany(i => i.Courses)
                .UsingEntity<Dictionary<string, object>>(
                    "CourseInstructor",
                    right => right.HasOne<Instructor>().WithMany().HasForeignKey("InstructorID"),
                    left => left.HasOne<Course>().WithMany().HasForeignKey("CourseID"),
                    join =>
                    {
                        join.ToTable("CourseInstructor");
                        join.HasKey("CourseID", "InstructorID");
                    });
        }
    }
}
