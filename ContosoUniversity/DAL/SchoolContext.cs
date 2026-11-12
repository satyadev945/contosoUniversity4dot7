using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

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

            modelBuilder.Entity<Course>()
                .HasMany(c => c.Instructors).WithMany(i => i.Courses)
                .UsingEntity<Dictionary<string, object>>(
                    "CourseInstructor",
                    r => r.HasOne<Instructor>().WithMany().HasForeignKey("InstructorID"),
                    l => l.HasOne<Course>().WithMany().HasForeignKey("CourseID"),
                    j =>
                    {
                        j.HasKey("CourseID", "InstructorID");
                        j.ToTable("CourseInstructor");
                    });

            modelBuilder.Entity<Department>()
                .HasOne(d => d.Administrator).WithMany().HasForeignKey(d => d.InstructorID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public static class SchoolInitializer
    {
        public static void Initialize(SchoolContext context)
        {
            context.Database.EnsureCreated();

            if (context.Students.Any())
            {
                return;
            }

            var students = new List<Student>
            {
                new Student{FirstMidName="Carson",LastName="Alexander",EnrollmentDate=DateTime.Parse("2005-09-01")},
                new Student{FirstMidName="Meredith",LastName="Alonso",EnrollmentDate=DateTime.Parse("2002-09-01")},
                new Student{FirstMidName="Arturo",LastName="Anand",EnrollmentDate=DateTime.Parse("2003-09-01")},
                new Student{FirstMidName="Gytis",LastName="Barzdukas",EnrollmentDate=DateTime.Parse("2002-09-01")},
                new Student{FirstMidName="Yan",LastName="Li",EnrollmentDate=DateTime.Parse("2002-09-01")},
                new Student{FirstMidName="Peggy",LastName="Justice",EnrollmentDate=DateTime.Parse("2001-09-01")},
                new Student{FirstMidName="Laura",LastName="Norman",EnrollmentDate=DateTime.Parse("2003-09-01")},
                new Student{FirstMidName="Nino",LastName="Olivetto",EnrollmentDate=DateTime.Parse("2005-09-01")}
            };
            context.Students.AddRange(students);
            context.SaveChanges();

            var departments = new List<Department>
            {
                new Department{Name="General", Budget=100000, StartDate=DateTime.Parse("2000-09-01")}
            };
            context.Departments.AddRange(departments);
            context.SaveChanges();
            int departmentId = departments[0].DepartmentID;

            var courses = new List<Course>
            {
                new Course{CourseID=1050,Title="Chemistry",Credits=3,DepartmentID=departmentId},
                new Course{CourseID=4022,Title="Microeconomics",Credits=3,DepartmentID=departmentId},
                new Course{CourseID=4041,Title="Macroeconomics",Credits=3,DepartmentID=departmentId},
                new Course{CourseID=1045,Title="Calculus",Credits=4,DepartmentID=departmentId},
                new Course{CourseID=3141,Title="Trigonometry",Credits=4,DepartmentID=departmentId},
                new Course{CourseID=2021,Title="Composition",Credits=3,DepartmentID=departmentId},
                new Course{CourseID=2042,Title="Literature",Credits=4,DepartmentID=departmentId}
            };
            context.Courses.AddRange(courses);
            context.SaveChanges();

            var enrollments = new List<Enrollment>
            {
                new Enrollment{StudentID=students[0].ID,CourseID=1050,Grade=Grade.A},
                new Enrollment{StudentID=students[0].ID,CourseID=4022,Grade=Grade.C},
                new Enrollment{StudentID=students[0].ID,CourseID=4041,Grade=Grade.B},
                new Enrollment{StudentID=students[1].ID,CourseID=1045,Grade=Grade.B},
                new Enrollment{StudentID=students[1].ID,CourseID=3141,Grade=Grade.F},
                new Enrollment{StudentID=students[1].ID,CourseID=2021,Grade=Grade.F},
                new Enrollment{StudentID=students[2].ID,CourseID=1050},
                new Enrollment{StudentID=students[3].ID,CourseID=1050},
                new Enrollment{StudentID=students[3].ID,CourseID=4022,Grade=Grade.F},
                new Enrollment{StudentID=students[4].ID,CourseID=4041,Grade=Grade.C},
                new Enrollment{StudentID=students[5].ID,CourseID=1045},
                new Enrollment{StudentID=students[6].ID,CourseID=3141,Grade=Grade.A}
            };
            context.Enrollments.AddRange(enrollments);
            context.SaveChanges();
        }
    }
}
