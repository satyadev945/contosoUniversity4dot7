using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// Seeds the database with the sample data that was previously created by the EF6
    /// migrations Configuration.Seed method. The seed is idempotent and only runs when the
    /// database does not contain any students yet.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(SchoolContext context, CancellationToken cancellationToken = default)
        {
            await context.Database.MigrateAsync(cancellationToken);

            if (await context.Students.AnyAsync(cancellationToken))
            {
                return;
            }

            var students = new List<Student>
            {
                new() { FirstMidName = "Carson",   LastName = "Alexander", EnrollmentDate = DateTime.Parse("2010-09-01") },
                new() { FirstMidName = "Meredith", LastName = "Alonso",    EnrollmentDate = DateTime.Parse("2012-09-01") },
                new() { FirstMidName = "Arturo",   LastName = "Anand",     EnrollmentDate = DateTime.Parse("2013-09-01") },
                new() { FirstMidName = "Gytis",    LastName = "Barzdukas", EnrollmentDate = DateTime.Parse("2012-09-01") },
                new() { FirstMidName = "Yan",      LastName = "Li",        EnrollmentDate = DateTime.Parse("2012-09-01") },
                new() { FirstMidName = "Peggy",    LastName = "Justice",   EnrollmentDate = DateTime.Parse("2011-09-01") },
                new() { FirstMidName = "Laura",    LastName = "Norman",    EnrollmentDate = DateTime.Parse("2013-09-01") },
                new() { FirstMidName = "Nino",     LastName = "Olivetto",  EnrollmentDate = DateTime.Parse("2005-09-01") }
            };
            context.Students.AddRange(students);

            var instructors = new List<Instructor>
            {
                new() { FirstMidName = "Kim",     LastName = "Abercrombie", HireDate = DateTime.Parse("1995-03-11") },
                new() { FirstMidName = "Fadi",    LastName = "Fakhouri",    HireDate = DateTime.Parse("2002-07-06") },
                new() { FirstMidName = "Roger",   LastName = "Harui",       HireDate = DateTime.Parse("1998-07-01") },
                new() { FirstMidName = "Candace", LastName = "Kapoor",      HireDate = DateTime.Parse("2001-01-15") },
                new() { FirstMidName = "Roger",   LastName = "Zheng",       HireDate = DateTime.Parse("2004-02-12") }
            };
            context.Instructors.AddRange(instructors);
            await context.SaveChangesAsync(cancellationToken);

            Instructor Instructor(string lastName) => instructors.Single(i => i.LastName == lastName);

            var departments = new List<Department>
            {
                new() { Name = "English",     Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = Instructor("Abercrombie").ID },
                new() { Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = Instructor("Fakhouri").ID },
                new() { Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = Instructor("Harui").ID },
                new() { Name = "Economics",   Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = Instructor("Kapoor").ID }
            };
            context.Departments.AddRange(departments);
            await context.SaveChangesAsync(cancellationToken);

            int DepartmentId(string name) => departments.Single(d => d.Name == name).DepartmentID;

            var courses = new List<Course>
            {
                new() { CourseID = 1050, Title = "Chemistry",      Credits = 3, DepartmentID = DepartmentId("Engineering"), Instructors = new List<Instructor>() },
                new() { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = DepartmentId("Economics"),   Instructors = new List<Instructor>() },
                new() { CourseID = 4041, Title = "Macroeconomics", Credits = 3, DepartmentID = DepartmentId("Economics"),   Instructors = new List<Instructor>() },
                new() { CourseID = 1045, Title = "Calculus",       Credits = 4, DepartmentID = DepartmentId("Mathematics"), Instructors = new List<Instructor>() },
                new() { CourseID = 3141, Title = "Trigonometry",   Credits = 4, DepartmentID = DepartmentId("Mathematics"), Instructors = new List<Instructor>() },
                new() { CourseID = 2021, Title = "Composition",    Credits = 3, DepartmentID = DepartmentId("English"),     Instructors = new List<Instructor>() },
                new() { CourseID = 2042, Title = "Literature",     Credits = 4, DepartmentID = DepartmentId("English"),     Instructors = new List<Instructor>() }
            };

            void Assign(string courseTitle, string instructorName)
            {
                var course = courses.Single(c => c.Title == courseTitle);
                course.Instructors!.Add(Instructor(instructorName));
            }

            Assign("Chemistry", "Kapoor");
            Assign("Chemistry", "Harui");
            Assign("Microeconomics", "Zheng");
            Assign("Macroeconomics", "Zheng");
            Assign("Calculus", "Fakhouri");
            Assign("Trigonometry", "Harui");
            Assign("Composition", "Abercrombie");
            Assign("Literature", "Abercrombie");

            context.Courses.AddRange(courses);

            context.OfficeAssignments.AddRange(
                new OfficeAssignment { InstructorID = Instructor("Fakhouri").ID, Location = "Smith 17" },
                new OfficeAssignment { InstructorID = Instructor("Harui").ID,    Location = "Gowan 27" },
                new OfficeAssignment { InstructorID = Instructor("Kapoor").ID,   Location = "Thompson 304" });
            await context.SaveChangesAsync(cancellationToken);

            int StudentId(string lastName) => students.Single(s => s.LastName == lastName).ID;
            int CourseId(string title) => courses.Single(c => c.Title == title).CourseID;

            context.Enrollments.AddRange(
                new Enrollment { StudentID = StudentId("Alexander"), CourseID = CourseId("Chemistry"),      Grade = Grade.A },
                new Enrollment { StudentID = StudentId("Alexander"), CourseID = CourseId("Microeconomics"), Grade = Grade.C },
                new Enrollment { StudentID = StudentId("Alexander"), CourseID = CourseId("Macroeconomics"), Grade = Grade.B },
                new Enrollment { StudentID = StudentId("Alonso"),    CourseID = CourseId("Calculus"),       Grade = Grade.B },
                new Enrollment { StudentID = StudentId("Alonso"),    CourseID = CourseId("Trigonometry"),   Grade = Grade.B },
                new Enrollment { StudentID = StudentId("Alonso"),    CourseID = CourseId("Composition"),    Grade = Grade.B },
                new Enrollment { StudentID = StudentId("Anand"),     CourseID = CourseId("Chemistry") },
                new Enrollment { StudentID = StudentId("Anand"),     CourseID = CourseId("Microeconomics"), Grade = Grade.B },
                new Enrollment { StudentID = StudentId("Barzdukas"), CourseID = CourseId("Chemistry"),      Grade = Grade.B },
                new Enrollment { StudentID = StudentId("Li"),        CourseID = CourseId("Composition"),    Grade = Grade.B },
                new Enrollment { StudentID = StudentId("Justice"),   CourseID = CourseId("Literature"),     Grade = Grade.B });
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
