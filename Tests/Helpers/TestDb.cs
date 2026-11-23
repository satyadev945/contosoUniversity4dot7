using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ContosoUniversity.Tests.Helpers
{
    public class ThrowingSchoolContext : SchoolContext
    {
        private readonly Exception _exception;

        public ThrowingSchoolContext(DbContextOptions<SchoolContext> options, Exception exception) : base(options)
        {
            _exception = exception;
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            throw _exception;
        }
    }

    /// <summary>
    /// The shipped migration targets SQL Server (SqlServer:Identity). This generator maps those identity
    /// columns to SQLite autoincrement so that the real migration can run against SQLite in tests.
    /// </summary>
    public class IdentityAwareSqliteMigrationsSqlGenerator : Microsoft.EntityFrameworkCore.Migrations.SqliteMigrationsSqlGenerator
    {
        public IdentityAwareSqliteMigrationsSqlGenerator(
            Microsoft.EntityFrameworkCore.Migrations.MigrationsSqlGeneratorDependencies dependencies,
            Microsoft.EntityFrameworkCore.Metadata.IRelationalAnnotationProvider migrationsAnnotations)
            : base(dependencies, migrationsAnnotations)
        {
        }

        protected override void Generate(
            Microsoft.EntityFrameworkCore.Migrations.Operations.CreateTableOperation operation,
            Microsoft.EntityFrameworkCore.Metadata.IModel model,
            Microsoft.EntityFrameworkCore.Migrations.MigrationCommandListBuilder builder,
            bool terminate = true)
        {
            foreach (var column in operation.Columns)
            {
                if (column["SqlServer:Identity"] != null)
                {
                    column.ColumnType = "INTEGER";
                    column["Sqlite:Autoincrement"] = true;
                }
            }
            base.Generate(operation, model, builder, terminate);
        }
    }

    public static class TestDb
    {
        public static DbContextOptions<SchoolContext> InMemoryOptions(string name = null)
        {
            return new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
        }

        public static SchoolContext CreateInMemory(string name = null)
        {
            return new SchoolContext(InMemoryOptions(name));
        }

        /// <summary>Seeds a small data set. Returns the database name used.</summary>
        public static string SeedBasic()
        {
            var name = Guid.NewGuid().ToString();
            using var ctx = CreateInMemory(name);

            var i1 = new Instructor { ID = 1, FirstMidName = "Kim", LastName = "Abercrombie", HireDate = new DateTime(1995, 3, 11) };
            var i2 = new Instructor { ID = 2, FirstMidName = "Fadi", LastName = "Fakhouri", HireDate = new DateTime(2002, 7, 6) };
            ctx.Instructors.AddRange(i1, i2);

            ctx.Departments.AddRange(
                new Department { DepartmentID = 1, Name = "English", Budget = 1000, StartDate = new DateTime(2007, 9, 1), InstructorID = 1 },
                new Department { DepartmentID = 2, Name = "Mathematics", Budget = 2000, StartDate = new DateTime(2007, 9, 1), InstructorID = 2 });

            ctx.Courses.AddRange(
                new Course { CourseID = 1045, Title = "Calculus", Credits = 4, DepartmentID = 2, Instructors = new List<Instructor> { i2 } },
                new Course { CourseID = 2021, Title = "Composition", Credits = 3, DepartmentID = 1, Instructors = new List<Instructor> { i1 } },
                new Course { CourseID = 2042, Title = "Literature", Credits = 4, DepartmentID = 1, Instructors = new List<Instructor> { i1 } });

            ctx.OfficeAssignments.Add(new OfficeAssignment { InstructorID = 1, Location = "Smith 17" });

            ctx.Students.AddRange(
                new Student { ID = 1, FirstMidName = "Carson", LastName = "Alexander", EnrollmentDate = new DateTime(2010, 9, 1) },
                new Student { ID = 2, FirstMidName = "Meredith", LastName = "Alonso", EnrollmentDate = new DateTime(2012, 9, 1) },
                new Student { ID = 3, FirstMidName = "Arturo", LastName = "Anand", EnrollmentDate = new DateTime(2012, 9, 1) },
                new Student { ID = 4, FirstMidName = "Gytis", LastName = "Barzdukas", EnrollmentDate = new DateTime(2013, 9, 1) },
                new Student { ID = 5, FirstMidName = "Yan", LastName = "Li", EnrollmentDate = new DateTime(2005, 9, 1) });

            ctx.Enrollments.AddRange(
                new Enrollment { EnrollmentID = 1, StudentID = 1, CourseID = 1045, Grade = Grade.A },
                new Enrollment { EnrollmentID = 2, StudentID = 2, CourseID = 2021, Grade = Grade.B },
                new Enrollment { EnrollmentID = 3, StudentID = 3, CourseID = 2021 });

            ctx.SaveChanges();
            return name;
        }

        public static SqliteConnection OpenSqlite()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            return connection;
        }

        public static SchoolContext CreateSqlite(SqliteConnection connection, params IInterceptor[] interceptors)
        {
            var builder = new DbContextOptionsBuilder<SchoolContext>().UseSqlite(connection)
                .ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, IdentityAwareSqliteMigrationsSqlGenerator>()
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            if (interceptors.Length > 0)
            {
                builder.AddInterceptors(interceptors);
            }
            return new SchoolContext(builder.Options);
        }
    }
}
