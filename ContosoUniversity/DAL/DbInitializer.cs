using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.DAL
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(SchoolContext context)
        {
            if ((await context.Database.GetPendingMigrationsAsync()).Any() || context.Database.GetMigrations().Any())
            {
                await context.Database.MigrateAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            if (await context.Students.AnyAsync())
            {
                return;
            }

            var students = new List<Student>
            {
                new Student { FirstMidName = "Carson", LastName = "Alexander", EnrollmentDate = DateTime.Parse("2005-09-01") },
                new Student { FirstMidName = "Meredith", LastName = "Alonso", EnrollmentDate = DateTime.Parse("2002-09-01") },
                new Student { FirstMidName = "Arturo", LastName = "Anand", EnrollmentDate = DateTime.Parse("2003-09-01") },
                new Student { FirstMidName = "Gytis", LastName = "Barzdukas", EnrollmentDate = DateTime.Parse("2002-09-01") },
                new Student { FirstMidName = "Yan", LastName = "Li", EnrollmentDate = DateTime.Parse("2002-09-01") },
                new Student { FirstMidName = "Peggy", LastName = "Justice", EnrollmentDate = DateTime.Parse("2001-09-01") },
                new Student { FirstMidName = "Laura", LastName = "Norman", EnrollmentDate = DateTime.Parse("2003-09-01") },
                new Student { FirstMidName = "Nino", LastName = "Olivetto", EnrollmentDate = DateTime.Parse("2005-09-01") }
            };
            context.Students.AddRange(students);
            await context.SaveChangesAsync();

            var instructors = new List<Instructor>
            {
                new Instructor { FirstMidName = "Kim", LastName = "Abercrombie", HireDate = DateTime.Parse("1995-03-11") },
                new Instructor { FirstMidName = "Fadi", LastName = "Fakhouri", HireDate = DateTime.Parse("2002-07-06") },
                new Instructor { FirstMidName = "Roger", LastName = "Harui", HireDate = DateTime.Parse("1998-07-01") },
                new Instructor { FirstMidName = "Candace", LastName = "Kapoor", HireDate = DateTime.Parse("2001-01-15") },
                new Instructor { FirstMidName = "Roger", LastName = "Zheng", HireDate = DateTime.Parse("2004-02-12") }
            };
            context.Instructors.AddRange(instructors);
            await context.SaveChangesAsync();

            var departments = new List<Department>
            {
                new Department { Name = "English", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors[0].ID },
                new Department { Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors[1].ID },
                new Department { Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors[2].ID },
                new Department { Name = "Economics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors[3].ID }
            };
            context.Departments.AddRange(departments);
            await context.SaveChangesAsync();

            var english = departments.Single(d => d.Name == "English");
            var mathematics = departments.Single(d => d.Name == "Mathematics");
            var engineering = departments.Single(d => d.Name == "Engineering");
            var economics = departments.Single(d => d.Name == "Economics");

            var courses = new List<Course>
            {
                new Course { CourseID = 1050, Title = "Chemistry", Credits = 3, DepartmentID = engineering.DepartmentID },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = economics.DepartmentID },
                new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, DepartmentID = economics.DepartmentID },
                new Course { CourseID = 1045, Title = "Calculus", Credits = 4, DepartmentID = mathematics.DepartmentID },
                new Course { CourseID = 3141, Title = "Trigonometry", Credits = 4, DepartmentID = mathematics.DepartmentID },
                new Course { CourseID = 2021, Title = "Composition", Credits = 3, DepartmentID = english.DepartmentID },
                new Course { CourseID = 2042, Title = "Literature", Credits = 4, DepartmentID = english.DepartmentID }
            };
            context.Courses.AddRange(courses);
            await context.SaveChangesAsync();

            var offices = new List<OfficeAssignment>
            {
                new OfficeAssignment { InstructorID = instructors[1].ID, Location = "Smith 17" },
                new OfficeAssignment { InstructorID = instructors[2].ID, Location = "Gowan 27" },
                new OfficeAssignment { InstructorID = instructors[3].ID, Location = "Thompson 304" }
            };
            context.OfficeAssignments.AddRange(offices);

            Course C(int id) => courses.Single(c => c.CourseID == id);
            C(1050).Instructors = new List<Instructor> { instructors[0], instructors[3] };
            C(4022).Instructors = new List<Instructor> { instructors[3] };
            C(4041).Instructors = new List<Instructor> { instructors[3] };
            C(1045).Instructors = new List<Instructor> { instructors[1] };
            C(3141).Instructors = new List<Instructor> { instructors[2] };
            C(2021).Instructors = new List<Instructor> { instructors[2] };
            C(2042).Instructors = new List<Instructor> { instructors[4] };
            await context.SaveChangesAsync();

            var enrollments = new List<Enrollment>
            {
                new Enrollment { StudentID = students[0].ID, CourseID = 1050, Grade = Grade.A },
                new Enrollment { StudentID = students[0].ID, CourseID = 4022, Grade = Grade.C },
                new Enrollment { StudentID = students[0].ID, CourseID = 4041, Grade = Grade.B },
                new Enrollment { StudentID = students[1].ID, CourseID = 1045, Grade = Grade.B },
                new Enrollment { StudentID = students[1].ID, CourseID = 3141, Grade = Grade.F },
                new Enrollment { StudentID = students[1].ID, CourseID = 2021, Grade = Grade.F },
                new Enrollment { StudentID = students[2].ID, CourseID = 1050 },
                new Enrollment { StudentID = students[3].ID, CourseID = 1050 },
                new Enrollment { StudentID = students[3].ID, CourseID = 4022, Grade = Grade.F },
                new Enrollment { StudentID = students[4].ID, CourseID = 4041, Grade = Grade.C },
                new Enrollment { StudentID = students[5].ID, CourseID = 1045 },
                new Enrollment { StudentID = students[6].ID, CourseID = 3141, Grade = Grade.A }
            };
            context.Enrollments.AddRange(enrollments);
            await context.SaveChangesAsync();
        }
    }
}
