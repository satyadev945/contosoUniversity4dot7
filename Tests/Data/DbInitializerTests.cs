using System;
using System.Linq;
using ContosoUniversity.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.Data
{
    public class DbInitializerTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly SchoolContext _context;

        public DbInitializerTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            var options = new DbContextOptionsBuilder<SchoolContext>().UseSqlite(_connection)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                .Options;
            _context = new SchoolContext(options);

            // The SQL Server migrations cannot run on SQLite (identity columns), so build the schema
            // from the model and mark all migrations as applied; Initialize then only seeds.
            _context.Database.EnsureCreated();
            _context.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL)");
            foreach (var migration in _context.Database.GetMigrations())
            {
                _context.Database.ExecuteSqlRaw("INSERT INTO \"__EFMigrationsHistory\" VALUES ({0}, '10.0.0')", migration);
            }
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public void Initialize_OnEmptyDatabase_SeedsAllEntities()
        {
            DbInitializer.Initialize(_context);

            Assert.Equal(8, _context.Students.Count());
            Assert.Equal(5, _context.Instructors.Count());
            Assert.Equal(4, _context.Departments.Count());
            Assert.Equal(7, _context.Courses.Count());
            Assert.Equal(3, _context.OfficeAssignments.Count());
            Assert.Equal(11, _context.Enrollments.Count());
        }

        [Fact]
        public void Initialize_SeedsCourseInstructorRelationships()
        {
            DbInitializer.Initialize(_context);

            var chemistry = _context.Courses.Include(c => c.Instructors).Single(c => c.Title == "Chemistry");
            Assert.Equal(2, chemistry.Instructors.Count);
            Assert.Equal("Engineering", _context.Departments.Single(d => d.DepartmentID == chemistry.DepartmentID).Name);
        }

        [Fact]
        public void Initialize_SeedsEnrollmentGrades()
        {
            DbInitializer.Initialize(_context);

            Assert.Single(_context.Enrollments.Where(e => e.Grade == null));
            Assert.Equal(Grade.A, _context.Enrollments
                .Single(e => e.Student.LastName == "Alexander" && e.Course.Title == "Chemistry").Grade);
        }

        [Fact]
        public void Initialize_CalledTwice_DoesNotDuplicateData()
        {
            DbInitializer.Initialize(_context);
            DbInitializer.Initialize(_context);

            Assert.Equal(8, _context.Students.Count());
            Assert.Equal(5, _context.Instructors.Count());
        }

        [Fact]
        public void Initialize_WhenStudentsExist_DoesNotSeed()
        {
            DbInitializer.Initialize(_context);
            _context.Enrollments.RemoveRange(_context.Enrollments);
            _context.SaveChanges();

            DbInitializer.Initialize(_context);

            Assert.Empty(_context.Enrollments);
        }

        [Fact]
        public void Initialize_NullContext_Throws()
        {
            Assert.Throws<NullReferenceException>(() => DbInitializer.Initialize(null));
        }
    }
}
