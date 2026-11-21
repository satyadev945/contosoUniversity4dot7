using ContosoUniversity.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Tests
{
    /// <summary>
    /// Creates an isolated SQLite in-memory database with the SchoolContext schema.
    /// </summary>
    public sealed class TestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;

        public TestDatabase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseSqlite(_connection)
                .Options;

            Context = new SchoolContext(options);
            Context.Database.EnsureCreated();
        }

        public SchoolContext Context { get; }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
