using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.DAL
{
    /// <summary>
    /// Applies pending migrations and seeds the database with sample data when it is empty.
    /// Replaces EF6 SchoolInitializer / Migrations.Configuration.Seed (AddOrUpdate is not available in EF Core).
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(SchoolContext context, ILogger logger)
        {
            await context.Database.MigrateAsync();

            if (await context.Students.AnyAsync())
            {
                logger.LogInformation("Database already contains data; seeding skipped.");
                return;
            }

            logger.LogInformation("Seeding database with sample data.");

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

            Instructor Inst(string lastName) => instructors.Single(i => i.LastName == lastName);

            var departments = new List<Department>
            {
                new Department { Name = "English",     Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = Inst("Abercrombie").ID },
                new Department { Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = Inst("Fakhouri").ID },
                new Department { Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = Inst("Harui").ID },
                new Department { Name = "Economics",   Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = Inst("Kapoor").ID }
            };
            context.Departments.AddRange(departments);
            await context.SaveChangesAsync();

            int Dept(string name) => departments.Single(d => d.Name == name).DepartmentID;

            var courses = new List<Course>
            {
                new Course { CourseID = 1050, Title = "Chemistry",      Credits = 3, DepartmentID = Dept("Engineering"), Instructors = new List<Instructor> { Inst("Kapoor"), Inst("Harui") } },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = Dept("Economics"),   Instructors = new List<Instructor> { Inst("Zheng") } },
                new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, DepartmentID = Dept("Economics"),   Instructors = new List<Instructor> { Inst("Zheng") } },
                new Course { CourseID = 1045, Title = "Calculus",       Credits = 4, DepartmentID = Dept("Mathematics"), Instructors = new List<Instructor> { Inst("Fakhouri") } },
                new Course { CourseID = 3141, Title = "Trigonometry",   Credits = 4, DepartmentID = Dept("Mathematics"), Instructors = new List<Instructor> { Inst("Harui") } },
                new Course { CourseID = 2021, Title = "Composition",    Credits = 3, DepartmentID = Dept("English"),     Instructors = new List<Instructor> { Inst("Abercrombie") } },
                new Course { CourseID = 2042, Title = "Literature",     Credits = 4, DepartmentID = Dept("English"),     Instructors = new List<Instructor> { Inst("Abercrombie") } }
            };
            context.Courses.AddRange(courses);
            await context.SaveChangesAsync();

            context.OfficeAssignments.AddRange(
                new OfficeAssignment { InstructorID = Inst("Fakhouri").ID, Location = "Smith 17" },
                new OfficeAssignment { InstructorID = Inst("Harui").ID,    Location = "Gowan 27" },
                new OfficeAssignment { InstructorID = Inst("Kapoor").ID,   Location = "Thompson 304" });
            await context.SaveChangesAsync();

            int Stud(string lastName) => students.Single(s => s.LastName == lastName).ID;
            int Crs(string title) => courses.Single(c => c.Title == title).CourseID;

            context.Enrollments.AddRange(
                new Enrollment { StudentID = Stud("Alexander"), CourseID = Crs("Chemistry"),      Grade = Grade.A },
                new Enrollment { StudentID = Stud("Alexander"), CourseID = Crs("Microeconomics"), Grade = Grade.C },
                new Enrollment { StudentID = Stud("Alexander"), CourseID = Crs("Macroeconomics"), Grade = Grade.B },
                new Enrollment { StudentID = Stud("Alonso"),    CourseID = Crs("Calculus"),       Grade = Grade.B },
                new Enrollment { StudentID = Stud("Alonso"),    CourseID = Crs("Trigonometry"),   Grade = Grade.B },
                new Enrollment { StudentID = Stud("Alonso"),    CourseID = Crs("Composition"),    Grade = Grade.B },
                new Enrollment { StudentID = Stud("Anand"),     CourseID = Crs("Chemistry") },
                new Enrollment { StudentID = Stud("Anand"),     CourseID = Crs("Microeconomics"), Grade = Grade.B },
                new Enrollment { StudentID = Stud("Barzdukas"), CourseID = Crs("Chemistry"),      Grade = Grade.B },
                new Enrollment { StudentID = Stud("Li"),        CourseID = Crs("Composition"),    Grade = Grade.B },
                new Enrollment { StudentID = Stud("Justice"),   CourseID = Crs("Literature"),     Grade = Grade.B });
            await context.SaveChangesAsync();
        }
    }
}
