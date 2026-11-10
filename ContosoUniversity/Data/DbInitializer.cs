using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// Applies pending migrations and seeds the database when it is empty.
    /// Replaces the EF6 SchoolInitializer and Migrations.Configuration.Seed.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(SchoolContext context)
        {
            await context.Database.MigrateAsync();

            if (await context.Students.AnyAsync())
            {
                return;
            }

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
            context.Students.AddRange(students);
            await context.SaveChangesAsync();

            var instructors = new List<Instructor>
            {
                new Instructor { FirstMidName = "Kim",     LastName = "Abercrombie", HireDate = DateTime.Parse("1995-03-11") },
                new Instructor { FirstMidName = "Fadi",    LastName = "Fakhouri",    HireDate = DateTime.Parse("2002-07-06") },
                new Instructor { FirstMidName = "Roger",   LastName = "Harui",       HireDate = DateTime.Parse("1998-07-01") },
                new Instructor { FirstMidName = "Candace", LastName = "Kapoor",      HireDate = DateTime.Parse("2001-01-15") },
                new Instructor { FirstMidName = "Roger",   LastName = "Zheng",       HireDate = DateTime.Parse("2004-02-12") }
            };
            context.Instructors.AddRange(instructors);
            await context.SaveChangesAsync();

            var departments = new List<Department>
            {
                new Department { Name = "English",     Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single(i => i.LastName == "Abercrombie").ID },
                new Department { Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single(i => i.LastName == "Fakhouri").ID },
                new Department { Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single(i => i.LastName == "Harui").ID },
                new Department { Name = "Economics",   Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single(i => i.LastName == "Kapoor").ID }
            };
            context.Departments.AddRange(departments);
            await context.SaveChangesAsync();

            int DeptId(string name) => departments.Single(d => d.Name == name).DepartmentID;
            Instructor Inst(string lastName) => instructors.Single(i => i.LastName == lastName);

            var courses = new List<Course>
            {
                new Course { CourseID = 1050, Title = "Chemistry",      Credits = 3, DepartmentID = DeptId("Engineering"), Instructors = new List<Instructor> { Inst("Kapoor"), Inst("Harui") } },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = DeptId("Economics"),   Instructors = new List<Instructor> { Inst("Zheng") } },
                new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, DepartmentID = DeptId("Economics"),   Instructors = new List<Instructor> { Inst("Zheng") } },
                new Course { CourseID = 1045, Title = "Calculus",       Credits = 4, DepartmentID = DeptId("Mathematics"), Instructors = new List<Instructor> { Inst("Fakhouri") } },
                new Course { CourseID = 3141, Title = "Trigonometry",   Credits = 4, DepartmentID = DeptId("Mathematics"), Instructors = new List<Instructor> { Inst("Harui") } },
                new Course { CourseID = 2021, Title = "Composition",    Credits = 3, DepartmentID = DeptId("English"),     Instructors = new List<Instructor> { Inst("Abercrombie") } },
                new Course { CourseID = 2042, Title = "Literature",     Credits = 4, DepartmentID = DeptId("English"),     Instructors = new List<Instructor> { Inst("Abercrombie") } }
            };
            context.Courses.AddRange(courses);
            await context.SaveChangesAsync();

            var officeAssignments = new List<OfficeAssignment>
            {
                new OfficeAssignment { InstructorID = Inst("Fakhouri").ID, Location = "Smith 17" },
                new OfficeAssignment { InstructorID = Inst("Harui").ID,    Location = "Gowan 27" },
                new OfficeAssignment { InstructorID = Inst("Kapoor").ID,   Location = "Thompson 304" }
            };
            context.OfficeAssignments.AddRange(officeAssignments);
            await context.SaveChangesAsync();

            int StudentId(string lastName) => students.Single(s => s.LastName == lastName).ID;
            int CourseId(string title) => courses.Single(c => c.Title == title).CourseID;

            var enrollments = new List<Enrollment>
            {
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
                new Enrollment { StudentID = StudentId("Justice"),   CourseID = CourseId("Literature"),     Grade = Grade.B }
            };
            context.Enrollments.AddRange(enrollments);
            await context.SaveChangesAsync();
        }
    }
}
