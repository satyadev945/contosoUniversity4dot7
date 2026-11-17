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

            // Keep the singular table names used by the existing EF6 database
            // (the EF6 PluralizingTableNameConvention was removed in the legacy app).
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
                    right => right.HasOne<Instructor>().WithMany().HasForeignKey("InstructorID").OnDelete(DeleteBehavior.Cascade),
                    left => left.HasOne<Course>().WithMany().HasForeignKey("CourseID").OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.ToTable("CourseInstructor");
                        join.HasKey("CourseID", "InstructorID");
                    });

            modelBuilder.Entity<Department>()
                .HasOne(d => d.Administrator)
                .WithMany()
                .HasForeignKey(d => d.InstructorID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
