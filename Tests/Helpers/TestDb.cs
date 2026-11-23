using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoUniversity.Tests.Helpers
{
    /// <summary>SQLite in-memory database shared by all contexts created from one instance.</summary>
    public sealed class TestDb : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly SchoolInterceptorLogging? _interceptor;

        public TestDb(ILogger<SchoolInterceptorLogging>? interceptorLogger = null, bool ensureCreated = true)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            if (interceptorLogger != null)
            {
                _interceptor = new SchoolInterceptorLogging(interceptorLogger);
            }
            if (ensureCreated)
            {
                using var ctx = CreateContext();
                ctx.Database.EnsureCreated();
            }
        }

        public SchoolContext CreateContext()
        {
            var builder = new DbContextOptionsBuilder<SchoolContext>().UseSqlite(_connection)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            if (_interceptor != null)
            {
                builder.AddInterceptors(_interceptor);
            }
            return new SchoolContext(builder.Options);
        }

        public static ILogger<T> NullLog<T>() => NullLogger<T>.Instance;

        /// <summary>
        /// Instructors 1,2; Departments 1 (admin 1), 2 (admin 2); Courses 1050 (dept1; instr 1,2), 2021 (dept2; instr 2);
        /// Students 1..5; office assignment for instructor 1; several enrollments.
        /// </summary>
        public void Seed()
        {
            using var ctx = CreateContext();
            var i1 = new Instructor { ID = 1, LastName = "Abercrombie", FirstMidName = "Kim", HireDate = new DateTime(1995, 3, 11) };
            var i2 = new Instructor { ID = 2, LastName = "Fakhouri", FirstMidName = "Fadi", HireDate = new DateTime(2002, 7, 6) };
            ctx.Instructors.AddRange(i1, i2);
            ctx.Departments.AddRange(
                new Department { DepartmentID = 1, Name = "English", Budget = 350000, StartDate = new DateTime(2007, 9, 1), InstructorID = 1 },
                new Department { DepartmentID = 2, Name = "Mathematics", Budget = 100000, StartDate = new DateTime(2007, 9, 1), InstructorID = 2 });
            ctx.Courses.AddRange(
                new Course { CourseID = 1050, Title = "Chemistry", Credits = 3, DepartmentID = 1, Instructors = new List<Instructor> { i1, i2 } },
                new Course { CourseID = 2021, Title = "Composition", Credits = 4, DepartmentID = 2, Instructors = new List<Instructor> { i2 } });
            ctx.OfficeAssignments.Add(new OfficeAssignment { InstructorID = 1, Location = "Smith 17" });
            ctx.Students.AddRange(
                new Student { ID = 1, LastName = "Alexander", FirstMidName = "Carson", EnrollmentDate = new DateTime(2010, 9, 1) },
                new Student { ID = 2, LastName = "Alonso", FirstMidName = "Meredith", EnrollmentDate = new DateTime(2012, 9, 1) },
                new Student { ID = 3, LastName = "Anand", FirstMidName = "Arturo", EnrollmentDate = new DateTime(2013, 9, 1) },
                new Student { ID = 4, LastName = "Barzdukas", FirstMidName = "Gytis", EnrollmentDate = new DateTime(2012, 9, 1) },
                new Student { ID = 5, LastName = "Li", FirstMidName = "Yan", EnrollmentDate = new DateTime(2012, 9, 1) });
            ctx.SaveChanges();
            ctx.Enrollments.AddRange(
                new Enrollment { StudentID = 1, CourseID = 1050, Grade = Grade.A },
                new Enrollment { StudentID = 2, CourseID = 1050, Grade = Grade.B },
                new Enrollment { StudentID = 3, CourseID = 1050 },
                new Enrollment { StudentID = 1, CourseID = 2021, Grade = Grade.C });
            ctx.SaveChanges();
        }

        public void Dispose() => _connection.Dispose();
    }
}
