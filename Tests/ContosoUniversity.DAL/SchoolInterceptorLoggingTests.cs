using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ContosoUniversity.DAL
{
    public class SchoolInterceptorLoggingTests : IDisposable
    {
        private class CapturingLogger : ILogger<SchoolInterceptorLogging>
        {
            public List<(LogLevel Level, string Message, Exception Exception)> Entries { get; } = new();
            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Entries.Add((logLevel, formatter(state, exception), exception));
            }
        }

        private readonly SqliteConnection _conn;
        private readonly CapturingLogger _logger = new();
        private readonly SchoolContext _ctx;

        public SchoolInterceptorLoggingTests()
        {
            _conn = new SqliteConnection("DataSource=:memory:");
            _conn.Open();
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseSqlite(_conn)
                .AddInterceptors(new SchoolInterceptorLogging(_logger))
                .Options;
            _ctx = new SchoolContext(options);
            _ctx.Database.EnsureCreated();
            _logger.Entries.Clear();
        }

        public void Dispose()
        {
            _ctx.Dispose();
            _conn.Dispose();
        }

        [Fact]
        public void Constructor_AcceptsLogger()
        {
            Assert.NotNull(new SchoolInterceptorLogging(_logger));
        }

        [Fact]
        public void ReaderExecuted_LogsDebug()
        {
            _ctx.Students.ToList();
            Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("ReaderExecuted"));
        }

        [Fact]
        public async Task ReaderExecutedAsync_LogsDebug()
        {
            await _ctx.Students.ToListAsync();
            Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("ReaderExecuted"));
        }

        [Fact]
        public void NonQueryExecuted_LogsDebug()
        {
            _ctx.Database.ExecuteSqlRaw("DELETE FROM Student");
            Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("NonQueryExecuted"));
        }

        [Fact]
        public async Task NonQueryExecutedAsync_LogsDebug()
        {
            await _ctx.Database.ExecuteSqlRawAsync("DELETE FROM Student");
            Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("NonQueryExecuted"));
        }

        [Fact]
        public void ScalarExecuted_LogsDebug()
        {
            _ctx.Students.Count();
            Assert.Contains(_logger.Entries, e => e.Message.Contains("SELECT"));
        }

        [Fact]
        public void Logging_IncludesElapsedAndCommandText()
        {
            _ctx.Students.ToList();
            var entry = _logger.Entries.First(e => e.Message.Contains("ReaderExecuted"));
            Assert.Contains("ms", entry.Message);
            Assert.Contains("Student", entry.Message);
        }

        [Fact]
        public void CommandFailed_LogsError()
        {
            Assert.ThrowsAny<Exception>(() => _ctx.Database.ExecuteSqlRaw("THIS IS NOT SQL"));
            Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Error && e.Exception != null && e.Message.Contains("THIS IS NOT SQL"));
        }

        [Fact]
        public async Task CommandFailedAsync_LogsError()
        {
            await Assert.ThrowsAnyAsync<Exception>(() => _ctx.Database.ExecuteSqlRawAsync("THIS IS NOT SQL"));
            Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("THIS IS NOT SQL"));
        }

        [Fact]
        public void SaveChanges_IsLogged()
        {
            _ctx.Students.Add(new Student { LastName = "A", FirstMidName = "B", EnrollmentDate = DateTime.Today });
            _ctx.SaveChanges();
            Assert.Contains(_logger.Entries, e => e.Message.Contains("INSERT"));
        }

        [Fact]
        public void ScalarExecuted_DirectCommand_IsLogged()
        {
            // Raw scalar via the EF connection pipeline
            var count = _ctx.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM Student").ToList();
            Assert.Single(count);
            Assert.NotEmpty(_logger.Entries);
        }
    }
}
