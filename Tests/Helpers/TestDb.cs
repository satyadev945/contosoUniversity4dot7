using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace ContosoUniversity.Tests.Helpers
{
    /// <summary>SchoolContext whose SaveChangesAsync always throws the configured exception.</summary>
    public class ThrowingSchoolContext : SchoolContext
    {
        public ThrowingSchoolContext(DbContextOptions<SchoolContext> options) : base(options) { }

        public Exception ToThrow { get; set; } = new DbUpdateException("boom");

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw ToThrow;
        }
    }

    /// <summary>Logger that records formatted messages and exceptions.</summary>
    public class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception Exception)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }
    }

    /// <summary>SQLite in-memory backed database for tests.</summary>
    public sealed class TestDb : IDisposable
    {
        public SqliteConnection Connection { get; }
        public DbContextOptions<SchoolContext> Options { get; }

        public TestDb(bool createSchema = true, params IInterceptor[] interceptors)
        {
            Connection = new SqliteConnection("DataSource=:memory:");
            Connection.Open();
            var builder = new DbContextOptionsBuilder<SchoolContext>().UseSqlite(Connection)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            if (interceptors.Length > 0)
            {
                builder.AddInterceptors(interceptors);
            }
            Options = builder.Options;
            if (createSchema)
            {
                using var ctx = CreateContext();
                ctx.Database.EnsureCreated();
            }
        }

        public SchoolContext CreateContext() => new SchoolContext(Options);

        public ThrowingSchoolContext CreateThrowingContext(Exception ex = null)
        {
            var ctx = new ThrowingSchoolContext(Options);
            if (ex != null) ctx.ToThrow = ex;
            return ctx;
        }

        /// <summary>
        /// Records every migration as applied so MigrateAsync is a no-op
        /// (the SQL Server baseline migration cannot run against SQLite).
        /// </summary>
        public TestDb MarkMigrationsApplied()
        {
            using var ctx = CreateContext();
            ctx.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL)");
            foreach (var id in ctx.Database.GetMigrations())
            {
                ctx.Database.ExecuteSqlRaw("INSERT OR IGNORE INTO \"__EFMigrationsHistory\" VALUES ({0}, '10.0.0')", id);
            }
            return this;
        }

        public TestDb Seed()
        {
            using var ctx = CreateContext();
            var abercrombie = new Instructor { LastName = "Abercrombie", FirstMidName = "Kim", HireDate = new DateTime(1995, 3, 11) };
            var fakhouri = new Instructor { LastName = "Fakhouri", FirstMidName = "Fadi", HireDate = new DateTime(2002, 7, 6) };
            ctx.Instructors.AddRange(abercrombie, fakhouri);
            ctx.SaveChanges();

            var english = new Department { Name = "English", Budget = 350000, StartDate = new DateTime(2007, 9, 1), InstructorID = abercrombie.ID };
            var math = new Department { Name = "Mathematics", Budget = 100000, StartDate = new DateTime(2007, 9, 1), InstructorID = fakhouri.ID };
            ctx.Departments.AddRange(english, math);
            ctx.SaveChanges();

            var composition = new Course { CourseID = 2021, Title = "Composition", Credits = 3, DepartmentID = english.DepartmentID };
            composition.Instructors.Add(abercrombie);
            var calculus = new Course { CourseID = 1045, Title = "Calculus", Credits = 4, DepartmentID = math.DepartmentID };
            calculus.Instructors.Add(fakhouri);
            var orphan = new Course { CourseID = 3000, Title = "Unassigned", Credits = 2, DepartmentID = math.DepartmentID };
            ctx.Courses.AddRange(composition, calculus, orphan);
            ctx.OfficeAssignments.Add(new OfficeAssignment { InstructorID = fakhouri.ID, Location = "Smith 17" });
            ctx.SaveChanges();

            var students = new[]
            {
                new Student { LastName = "Alexander", FirstMidName = "Carson", EnrollmentDate = new DateTime(2010, 9, 1) },
                new Student { LastName = "Alonso", FirstMidName = "Meredith", EnrollmentDate = new DateTime(2012, 9, 1) },
                new Student { LastName = "Anand", FirstMidName = "Arturo", EnrollmentDate = new DateTime(2013, 9, 1) },
                new Student { LastName = "Li", FirstMidName = "Yan", EnrollmentDate = new DateTime(2012, 9, 1) },
                new Student { LastName = "Norman", FirstMidName = "Laura", EnrollmentDate = new DateTime(2013, 9, 1) }
            };
            ctx.Students.AddRange(students);
            ctx.SaveChanges();

            ctx.Enrollments.AddRange(
                new Enrollment { StudentID = students[0].ID, CourseID = 2021, Grade = Grade.A },
                new Enrollment { StudentID = students[1].ID, CourseID = 2021, Grade = Grade.B },
                new Enrollment { StudentID = students[1].ID, CourseID = 1045 });
            ctx.SaveChanges();
            return this;
        }

        public static void AttachFormContext(Controller controller, Dictionary<string, string> form)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddControllersWithViews();
            var sp = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = sp };
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            httpContext.Request.Form = new FormCollection(
                form.ToDictionary(kv => kv.Key, kv => new StringValues(kv.Value)));

            controller.ControllerContext = new ControllerContext(
                new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()))
            {
                ValueProviderFactories = new List<IValueProviderFactory> { new FormValueProviderFactory() }
            };
        }

        public void Dispose() => Connection.Dispose();
    }
}
