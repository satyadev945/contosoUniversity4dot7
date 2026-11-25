using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace ContosoUniversity.Tests.Helpers
{
    /// <summary>SchoolContext whose SaveChangesAsync can be forced to throw.</summary>
    public class ThrowingSchoolContext : SchoolContext
    {
        public Exception ToThrow { get; set; }

        public ThrowingSchoolContext(DbContextOptions<SchoolContext> options) : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            if (ToThrow != null)
            {
                throw ToThrow;
            }
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }

    public class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception Exception)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }
    }

    public static class TestDb
    {
        public static string NewName() => Guid.NewGuid().ToString();

        public static DbContextOptions<SchoolContext> Options(string name) =>
            new DbContextOptionsBuilder<SchoolContext>().UseInMemoryDatabase(name).Options;

        public static SchoolContext Create(string name) => new SchoolContext(Options(name));

        public static ThrowingSchoolContext CreateThrowing(string name, Exception toThrow) =>
            new ThrowingSchoolContext(Options(name)) { ToThrow = toThrow };

        /// <summary>Creates a seeded in-memory database and returns its name.</summary>
        public static string CreateSeeded()
        {
            var name = NewName();
            using var ctx = Create(name);
            Seed(ctx);
            return name;
        }

        public static void Seed(SchoolContext ctx)
        {
            var i1 = new Instructor { ID = 1, FirstMidName = "Kim", LastName = "Abercrombie", HireDate = new DateTime(1995, 3, 11) };
            var i2 = new Instructor { ID = 2, FirstMidName = "Fadi", LastName = "Fakhouri", HireDate = new DateTime(2002, 7, 6) };
            ctx.Instructors.AddRange(i1, i2);

            var d1 = new Department { DepartmentID = 1, Name = "Engineering", Budget = 350000, StartDate = new DateTime(2007, 9, 1), Administrator = i1 };
            var d2 = new Department { DepartmentID = 2, Name = "English", Budget = 100000, StartDate = new DateTime(2007, 9, 1), Administrator = i2 };
            ctx.Departments.AddRange(d1, d2);

            var c1 = new Course { CourseID = 1050, Title = "Chemistry", Credits = 3, Department = d1, Instructors = new List<Instructor> { i1 } };
            var c2 = new Course { CourseID = 2021, Title = "Composition", Credits = 3, Department = d2, Instructors = new List<Instructor> { i2 } };
            ctx.Courses.AddRange(c1, c2);

            ctx.OfficeAssignments.Add(new OfficeAssignment { Instructor = i1, Location = "Smith 17" });

            var s1 = new Student { ID = 1, FirstMidName = "Carson", LastName = "Alexander", EnrollmentDate = new DateTime(2010, 9, 1) };
            var s2 = new Student { ID = 2, FirstMidName = "Meredith", LastName = "Alonso", EnrollmentDate = new DateTime(2012, 9, 1) };
            var s3 = new Student { ID = 3, FirstMidName = "Arturo", LastName = "Anand", EnrollmentDate = new DateTime(2012, 9, 1) };
            var s4 = new Student { ID = 4, FirstMidName = "Gytis", LastName = "Barzdukas", EnrollmentDate = new DateTime(2013, 9, 1) };
            var s5 = new Student { ID = 5, FirstMidName = "Yan", LastName = "Li", EnrollmentDate = new DateTime(2013, 9, 1) };
            ctx.Students.AddRange(s1, s2, s3, s4, s5);

            ctx.Enrollments.AddRange(
                new Enrollment { EnrollmentID = 1, Student = s1, Course = c1, Grade = Grade.A },
                new Enrollment { EnrollmentID = 2, Student = s2, Course = c1, Grade = Grade.B },
                new Enrollment { EnrollmentID = 3, Student = s1, Course = c2 });

            ctx.SaveChanges();
        }

        /// <summary>
        /// Builds a ControllerContext backed by a real MVC service provider so that
        /// TryUpdateModelAsync works in unit tests.
        /// </summary>
        public static ControllerContext CreateControllerContext(Dictionary<string, string> form = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddControllersWithViews();
            var provider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = provider, TraceIdentifier = "trace-1" };
            httpContext.Request.Method = "POST";
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            var values = new Dictionary<string, StringValues>();
            if (form != null)
            {
                foreach (var kv in form)
                {
                    values[kv.Key] = kv.Value;
                }
            }
            httpContext.Request.Form = new FormCollection(values);

            var context = new ControllerContext(new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor()));
            context.ValueProviderFactories.Add(new FormValueProviderFactory());
            return context;
        }
    }
}
