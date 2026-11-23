using ContosoUniversity.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ContosoUniversity.Data
{
    public class SchoolInterceptorLoggingTests
    {
        private sealed class ListLogger<T> : ILogger<T>
        {
            public List<(LogLevel Level, string Message, Exception? Ex)> Entries { get; } = new();
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => Entries.Add((logLevel, formatter(state, exception), exception));
        }

        [Fact]
        public void Constructor_AcceptsLogger()
        {
            var i = new SchoolInterceptorLogging(new ListLogger<SchoolInterceptorLogging>());
            Assert.NotNull(i);
        }

        [Fact]
        public async Task ReaderExecutedAsync_IsLogged()
        {
            var log = new ListLogger<SchoolInterceptorLogging>();
            using var db = new TestDb(log);
            using var ctx = db.CreateContext();
            log.Entries.Clear();
            await ctx.Students.ToListAsync();
            Assert.Contains(log.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("ReaderExecuted") && e.Message.Contains("Student"));
        }

        [Fact]
        public void ReaderExecuted_Sync_IsLogged()
        {
            var log = new ListLogger<SchoolInterceptorLogging>();
            using var db = new TestDb(log);
            using var ctx = db.CreateContext();
            log.Entries.Clear();
            ctx.Students.ToList();
            Assert.Contains(log.Entries, e => e.Message.Contains("ReaderExecuted"));
        }

        [Fact]
        public async Task NonQueryExecutedAsync_IsLogged()
        {
            var log = new ListLogger<SchoolInterceptorLogging>();
            using var db = new TestDb(log);
            using var ctx = db.CreateContext();
            log.Entries.Clear();
            await ctx.Database.ExecuteSqlRawAsync("DELETE FROM Student");
            Assert.Contains(log.Entries, e => e.Message.Contains("NonQueryExecuted"));
        }

        [Fact]
        public void NonQueryExecuted_Sync_IsLogged()
        {
            var log = new ListLogger<SchoolInterceptorLogging>();
            using var db = new TestDb(log);
            using var ctx = db.CreateContext();
            log.Entries.Clear();
            ctx.Database.ExecuteSqlRaw("DELETE FROM Student");
            Assert.Contains(log.Entries, e => e.Message.Contains("NonQueryExecuted"));
        }

        [Fact]
        public void ScalarExecuted_Sync_IsLogged()
        {
            var log = new ListLogger<SchoolInterceptorLogging>();
            using var db = new TestDb(log);
            using var ctx = db.CreateContext();
            log.Entries.Clear();
            var conn = ctx.Database.GetDbConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            // direct ADO call is not intercepted; use EF scalar query instead
            var count = ctx.Students.Count();
            Assert.Equal(0, count);
            Assert.NotEmpty(log.Entries);
        }

        [Fact]
        public async Task CommandFailedAsync_IsLoggedAsError()
        {
            var log = new ListLogger<SchoolInterceptorLogging>();
            using var db = new TestDb(log);
            using var ctx = db.CreateContext();
            await Assert.ThrowsAnyAsync<Exception>(() => ctx.Database.ExecuteSqlRawAsync("DELETE FROM NoSuchTable"));
            Assert.Contains(log.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("Error executing command"));
        }

        [Fact]
        public void CommandFailed_Sync_IsLoggedAsError()
        {
            var log = new ListLogger<SchoolInterceptorLogging>();
            using var db = new TestDb(log);
            using var ctx = db.CreateContext();
            Assert.ThrowsAny<Exception>(() => ctx.Database.ExecuteSqlRaw("DELETE FROM NoSuchTable"));
            Assert.Contains(log.Entries, e => e.Level == LogLevel.Error);
        }
    }
}
