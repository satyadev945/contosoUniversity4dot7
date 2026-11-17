using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// Idempotent seeding that replaces the EF6 Configuration.Seed / AddOrUpdate logic.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(SchoolContext context, CancellationToken cancellationToken = default)
        {
            var students = new List<Student>
            {
                new Student { FirstMidName = "Carson",   LastName = "Alexander", EnrollmentDate = DateTime.Parse("2010-09-01") },
                new Student { FirstMidName = "Meredith", LastName = "Alonso",    EnrollmentDate = DateTime.Parse("2012-09-01") },
                new Student { FirstMidName = "Arturo",   LastName = "Anand",     EnrollmentDate = DateTime.Parse("2013-09-01") },
                new Student { FirstMidName = "Gytis",    LastName = "Barzdukas", EnrollmentDate = DateTime.Parse("2012-09-01") },
                new Student { FirstMidName = "Yan",      LastName = "Li",        EnrollmentDate = DateTime.Parse("2012-09-01") },
                new Student { FirstMidName = "Peggy",    LastName = "Justice",   EnrollmentDate = DateTime.Parse("2011-09-01") },
                new Student { FirstMidName = "Laura",    LastName = "Norman",    EnrollmentDate = DateTime.Parse("2013-09-01") },
                new Student { FirstMidName = "Nino",     LastName = "Olivetto",  EnrollmentDate = DateTime.Parse("2005-09-01") }
            };
            foreach (var s in students)
            {
                if (!await context.Students.AnyAsync(x => x.LastName == s.LastName, cancellationToken))
                {
                    context.Students.Add(s);
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            var instructors = new List<Instructor>
            {
                new Instructor { FirstMidName = "Kim",     LastName = "Abercrombie", HireDate = DateTime.Parse("1995-03-11") },
                new Instructor { FirstMidName = "Fadi",    LastName = "Fakhouri",    HireDate = DateTime.Parse("2002-07-06") },
                new Instructor { FirstMidName = "Roger",   LastName = "Harui",       HireDate = DateTime.Parse("1998-07-01") },
                new Instructor { FirstMidName = "Candace", LastName = "Kapoor",      HireDate = DateTime.Parse("2001-01-15") },
                new Instructor { FirstMidName = "Roger",   LastName = "Zheng",       HireDate = DateTime.Parse("2004-02-12") }
            };
            foreach (var i in instructors)
            {
                if (!await context.Instructors.AnyAsync(x => x.LastName == i.LastName, cancellationToken))
                {
                    context.Instructors.Add(i);
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            async Task<int> InstructorId(string lastName) =>
                (await context.Instructors.SingleAsync(i => i.LastName == lastName, cancellationToken)).ID;

            var departments = new List<Department>
            {
                new Department { Name = "English",     Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = await InstructorId("Abercrombie") },
                new Department { Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = await InstructorId("Fakhouri") },
                new Department { Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = await InstructorId("Harui") },
                new Department { Name = "Economics",   Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = await InstructorId("Kapoor") }
            };
            foreach (var d in departments)
            {
                if (!await context.Departments.AnyAsync(x => x.Name == d.Name, cancellationToken))
                {
                    context.Departments.Add(d);
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            async Task<int> DepartmentId(string name) =>
                (await context.Departments.SingleAsync(d => d.Name == name, cancellationToken)).DepartmentID;

            var courses = new List<Course>
            {
                new Course { CourseID = 1050, Title = "Chemistry",      Credits = 3, DepartmentID = await DepartmentId("Engineering") },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = await DepartmentId("Economics") },
                new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, DepartmentID = await DepartmentId("Economics") },
                new Course { CourseID = 1045, Title = "Calculus",       Credits = 4, DepartmentID = await DepartmentId("Mathematics") },
                new Course { CourseID = 3141, Title = "Trigonometry",   Credits = 4, DepartmentID = await DepartmentId("Mathematics") },
                new Course { CourseID = 2021, Title = "Composition",    Credits = 3, DepartmentID = await DepartmentId("English") },
                new Course { CourseID = 2042, Title = "Literature",     Credits = 4, DepartmentID = await DepartmentId("English") }
            };
            foreach (var c in courses)
            {
                if (!await context.Courses.AnyAsync(x => x.CourseID == c.CourseID, cancellationToken))
                {
                    context.Courses.Add(c);
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            var officeAssignments = new List<OfficeAssignment>
            {
                new OfficeAssignment { InstructorID = await InstructorId("Fakhouri"), Location = "Smith 17" },
                new OfficeAssignment { InstructorID = await InstructorId("Harui"),    Location = "Gowan 27" },
                new OfficeAssignment { InstructorID = await InstructorId("Kapoor"),   Location = "Thompson 304" }
            };
            foreach (var o in officeAssignments)
            {
                if (!await context.OfficeAssignments.AnyAsync(x => x.InstructorID == o.InstructorID, cancellationToken))
                {
                    context.OfficeAssignments.Add(o);
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            await AddInstructorToCourseAsync(context, "Chemistry", "Kapoor", cancellationToken);
            await AddInstructorToCourseAsync(context, "Chemistry", "Harui", cancellationToken);
            await AddInstructorToCourseAsync(context, "Microeconomics", "Zheng", cancellationToken);
            await AddInstructorToCourseAsync(context, "Macroeconomics", "Zheng", cancellationToken);
            await AddInstructorToCourseAsync(context, "Calculus", "Fakhouri", cancellationToken);
            await AddInstructorToCourseAsync(context, "Trigonometry", "Harui", cancellationToken);
            await AddInstructorToCourseAsync(context, "Composition", "Abercrombie", cancellationToken);
            await AddInstructorToCourseAsync(context, "Literature", "Abercrombie", cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var enrollmentSeeds = new (string Student, string Course, Grade? Grade)[]
            {
                ("Alexander", "Chemistry", Grade.A),
                ("Alexander", "Microeconomics", Grade.C),
                ("Alexander", "Macroeconomics", Grade.B),
                ("Alonso", "Calculus", Grade.B),
                ("Alonso", "Trigonometry", Grade.B),
                ("Alonso", "Composition", Grade.B),
                ("Anand", "Chemistry", null),
                ("Anand", "Microeconomics", Grade.B),
                ("Barzdukas", "Chemistry", Grade.B),
                ("Li", "Composition", Grade.B),
                ("Justice", "Literature", Grade.B)
            };
            foreach (var seed in enrollmentSeeds)
            {
                var studentId = (await context.Students.SingleAsync(s => s.LastName == seed.Student, cancellationToken)).ID;
                var courseId = (await context.Courses.SingleAsync(c => c.Title == seed.Course, cancellationToken)).CourseID;
                if (!await context.Enrollments.AnyAsync(e => e.StudentID == studentId && e.CourseID == courseId, cancellationToken))
                {
                    context.Enrollments.Add(new Enrollment { StudentID = studentId, CourseID = courseId, Grade = seed.Grade });
                }
            }
            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task AddInstructorToCourseAsync(SchoolContext context, string courseTitle, string instructorLastName, CancellationToken cancellationToken)
        {
            var course = await context.Courses.Include(c => c.Instructors).SingleAsync(c => c.Title == courseTitle, cancellationToken);
            course.Instructors ??= new List<Instructor>();
            if (!course.Instructors.Any(i => i.LastName == instructorLastName))
            {
                var instructor = await context.Instructors.SingleAsync(i => i.LastName == instructorLastName, cancellationToken);
                course.Instructors.Add(instructor);
            }
        }
    }
}
