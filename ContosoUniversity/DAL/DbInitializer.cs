using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.DAL
{
    /// <summary>
    /// Applies EF Core migrations (baselining databases created by the legacy EF6 version of the
    /// application) and seeds sample data idempotently.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(SchoolContext context)
        {
            await BaselineLegacyDatabaseAsync(context);
            await context.Database.MigrateAsync();
            await SeedAsync(context);
        }

        /// <summary>
        /// If the database was created by EF6 (tables exist, but no EF Core migration history), mark the
        /// initial EF Core migration as applied so that it does not try to re-create existing tables.
        /// </summary>
        private static async Task BaselineLegacyDatabaseAsync(SchoolContext context)
        {
            var initialMigration = context.Database.GetMigrations().FirstOrDefault();
            if (initialMigration == null)
            {
                return;
            }

            var productVersion = typeof(DbContext).Assembly.GetName().Version?.ToString(3) ?? "10.0.0";

            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[Student]', N'U') IS NOT NULL AND OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ({0}, {1});
END", initialMigration, productVersion);
        }

        private static async Task SeedAsync(SchoolContext context)
        {
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

            var abercrombie = new Instructor { FirstMidName = "Kim",     LastName = "Abercrombie", HireDate = DateTime.Parse("1995-03-11") };
            var fakhouri    = new Instructor { FirstMidName = "Fadi",    LastName = "Fakhouri",    HireDate = DateTime.Parse("2002-07-06") };
            var harui       = new Instructor { FirstMidName = "Roger",   LastName = "Harui",       HireDate = DateTime.Parse("1998-07-01") };
            var kapoor      = new Instructor { FirstMidName = "Candace", LastName = "Kapoor",      HireDate = DateTime.Parse("2001-01-15") };
            var zheng       = new Instructor { FirstMidName = "Roger",   LastName = "Zheng",       HireDate = DateTime.Parse("2004-02-12") };
            context.Instructors.AddRange(abercrombie, fakhouri, harui, kapoor, zheng);

            fakhouri.OfficeAssignment = new OfficeAssignment { Location = "Smith 17" };
            harui.OfficeAssignment = new OfficeAssignment { Location = "Gowan 27" };
            kapoor.OfficeAssignment = new OfficeAssignment { Location = "Thompson 304" };

            var english     = new Department { Name = "English",     Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), Administrator = abercrombie };
            var mathematics = new Department { Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), Administrator = fakhouri };
            var engineering = new Department { Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), Administrator = harui };
            var economics   = new Department { Name = "Economics",   Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), Administrator = kapoor };
            context.Departments.AddRange(english, mathematics, engineering, economics);

            var chemistry      = new Course { CourseID = 1050, Title = "Chemistry",      Credits = 3, Department = engineering,  Instructors = new List<Instructor> { kapoor, harui } };
            var microeconomics = new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, Department = economics,    Instructors = new List<Instructor> { zheng } };
            var macroeconomics = new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, Department = economics,    Instructors = new List<Instructor> { zheng } };
            var calculus       = new Course { CourseID = 1045, Title = "Calculus",       Credits = 4, Department = mathematics,  Instructors = new List<Instructor> { fakhouri } };
            var trigonometry   = new Course { CourseID = 3141, Title = "Trigonometry",   Credits = 4, Department = mathematics,  Instructors = new List<Instructor> { harui } };
            var composition    = new Course { CourseID = 2021, Title = "Composition",    Credits = 3, Department = english,      Instructors = new List<Instructor> { abercrombie } };
            var literature     = new Course { CourseID = 2042, Title = "Literature",     Credits = 4, Department = english,      Instructors = new List<Instructor> { abercrombie } };
            context.Courses.AddRange(chemistry, microeconomics, macroeconomics, calculus, trigonometry, composition, literature);

            Student Find(string lastName) => students.Single(s => s.LastName == lastName);

            context.Enrollments.AddRange(
                new Enrollment { Student = Find("Alexander"), Course = chemistry,      Grade = Grade.A },
                new Enrollment { Student = Find("Alexander"), Course = microeconomics, Grade = Grade.C },
                new Enrollment { Student = Find("Alexander"), Course = macroeconomics, Grade = Grade.B },
                new Enrollment { Student = Find("Alonso"),    Course = calculus,       Grade = Grade.B },
                new Enrollment { Student = Find("Alonso"),    Course = trigonometry,   Grade = Grade.B },
                new Enrollment { Student = Find("Alonso"),    Course = composition,    Grade = Grade.B },
                new Enrollment { Student = Find("Anand"),     Course = chemistry },
                new Enrollment { Student = Find("Anand"),     Course = microeconomics, Grade = Grade.B },
                new Enrollment { Student = Find("Barzdukas"), Course = chemistry,      Grade = Grade.B },
                new Enrollment { Student = Find("Li"),        Course = composition,    Grade = Grade.B },
                new Enrollment { Student = Find("Justice"),   Course = literature,     Grade = Grade.B });

            await context.SaveChangesAsync();
        }
    }
}
