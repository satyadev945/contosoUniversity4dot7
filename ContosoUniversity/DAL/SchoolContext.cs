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
            // Singular table names (EF6 PluralizingTableNameConvention was removed in the legacy model).
            modelBuilder.Entity<Course>().ToTable("Course");
            modelBuilder.Entity<Department>().ToTable("Department");
            modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
            modelBuilder.Entity<Instructor>().ToTable("Instructor");
            modelBuilder.Entity<Student>().ToTable("Student");
            modelBuilder.Entity<OfficeAssignment>().ToTable("OfficeAssignment");

            modelBuilder.Entity<Department>()
                .Property(d => d.Budget)
                .HasColumnType("money");

            // Department -> Administrator (optional): never cascade-delete the department when an instructor is removed.
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Administrator)
                .WithMany()
                .HasForeignKey(d => d.InstructorID)
                .OnDelete(DeleteBehavior.Restrict);

            // Course <-> Instructor many-to-many using the legacy CourseInstructor join table.
            modelBuilder.Entity<Course>()
                .HasMany(c => c.Instructors)
                .WithMany(i => i.Courses)
                .UsingEntity<Dictionary<string, object>>(
                    "CourseInstructor",
                    right => right.HasOne<Instructor>().WithMany().HasForeignKey("InstructorID").OnDelete(DeleteBehavior.Cascade),
                    left => left.HasOne<Course>().WithMany().HasForeignKey("CourseID").OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.HasKey("CourseID", "InstructorID");
                        join.ToTable("CourseInstructor");
                    });

            // Instructor 1:1 OfficeAssignment keyed by InstructorID.
            modelBuilder.Entity<OfficeAssignment>()
                .HasOne(o => o.Instructor)
                .WithOne(i => i.OfficeAssignment)
                .HasForeignKey<OfficeAssignment>(o => o.InstructorID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
