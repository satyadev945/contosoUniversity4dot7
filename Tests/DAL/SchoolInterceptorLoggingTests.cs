using System.Data.Common;
using ContosoUniversity.DAL;
using ContosoUniversity.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ContosoUniversity.DAL
{
    public class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception Exception)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }
    }

    public class SchoolInterceptorLoggingTests
    {
        private static CommandExecutedEventData Executed(DbCommand command, DbCommandMethod method)
        {
            return new CommandExecutedEventData(
                null, null, command.Connection, command, command.CommandText, null, method,
                Guid.NewGuid(), Guid.NewGuid(), null, false, false,
                DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(12), CommandSource.Unknown);
        }

        private static CommandErrorEventData Failed(DbCommand command, Exception ex)
        {
            return new CommandErrorEventData(
                null, null, command.Connection, command, command.CommandText, null, DbCommandMethod.ExecuteNonQuery,
                Guid.NewGuid(), Guid.NewGuid(), ex, false, false,
                DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(3), CommandSource.Unknown);
        }

        private static DbCommand MakeCommand()
        {
            var conn = new SqliteConnection("DataSource=:memory:");
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            return cmd;
        }

        [Fact]
        public void Constructor_CreatesInterceptor()
        {
            var i = new SchoolInterceptorLogging(new ListLogger<SchoolInterceptorLogging>());
            Assert.NotNull(i);
            Assert.IsAssignableFrom<DbCommandInterceptor>(i);
        }

        [Fact]
        public void ReaderExecuted_LogsAndReturnsResult()
        {
            var logger = new ListLogger<SchoolInterceptorLogging>();
            var i = new SchoolInterceptorLogging(logger);
            var cmd = MakeCommand();
            var result = i.ReaderExecuted(cmd, Executed(cmd, DbCommandMethod.ExecuteReader), null);
            Assert.Null(result);
            var entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Contains("ReaderExecuted", entry.Message);
            Assert.Contains("SELECT 1", entry.Message);
        }

        [Fact]
        public async Task ReaderExecutedAsync_Logs()
        {
            var logger = new ListLogger<SchoolInterceptorLogging>();
            var i = new SchoolInterceptorLogging(logger);
            var cmd = MakeCommand();
            await i.ReaderExecutedAsync(cmd, Executed(cmd, DbCommandMethod.ExecuteReader), null);
            Assert.Contains("ReaderExecuted", Assert.Single(logger.Entries).Message);
        }

        [Fact]
        public void NonQueryExecuted_LogsAndReturnsResult()
        {
            var logger = new ListLogger<SchoolInterceptorLogging>();
            var i = new SchoolInterceptorLogging(logger);
            var cmd = MakeCommand();
            Assert.Equal(7, i.NonQueryExecuted(cmd, Executed(cmd, DbCommandMethod.ExecuteNonQuery), 7));
            Assert.Contains("NonQueryExecuted", Assert.Single(logger.Entries).Message);
        }

        [Fact]
        public async Task NonQueryExecutedAsync_Logs()
        {
            var logger = new ListLogger<SchoolInterceptorLogging>();
            var i = new SchoolInterceptorLogging(logger);
            var cmd = MakeCommand();
            Assert.Equal(3, await i.NonQueryExecutedAsync(cmd, Executed(cmd, DbCommandMethod.ExecuteNonQuery), 3));
            Assert.Contains("NonQueryExecuted", Assert.Single(logger.Entries).Message);
        }

        [Fact]
        public void ScalarExecuted_LogsAndReturnsResult()
        {
            var logger = new ListLogger<SchoolInterceptorLogging>();
            var i = new SchoolInterceptorLogging(logger);
            var cmd = MakeCommand();
            Assert.Equal(5, i.ScalarExecuted(cmd, Executed(cmd, DbCommandMethod.ExecuteScalar), 5));
            Assert.Contains("ScalarExecuted", Assert.Single(logger.Entries).Message);
        }

        [Fact]
        public async Task ScalarExecutedAsync_Logs()
        {
            var logger = new ListLogger<SchoolInterceptorLogging>();
            var i = new SchoolInterceptorLogging(logger);
            var cmd = MakeCommand();
            Assert.Equal(9, await i.ScalarExecutedAsync(cmd, Executed(cmd, DbCommandMethod.ExecuteScalar), 9));
            Assert.Contains("ScalarExecuted", Assert.Single(logger.Entries).Message);
        }

        [Fact]
        public void CommandFailed_LogsError()
        {
            var logger = new ListLogger<SchoolInterceptorLogging>();
            var i = new SchoolInterceptorLogging(logger);
            var cmd = MakeCommand();
            var ex = new InvalidOperationException("boom");
            i.CommandFailed(cmd, Failed(cmd, ex));
            var entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Same(ex, entry.Exception);
            Assert.Contains("SELECT 1", entry.Message);
        }

        [Fact]
        public async Task CommandFailedAsync_LogsError()
        {
            var logger = new ListLogger<SchoolInterceptorLogging>();
            var i = new SchoolInterceptorLogging(logger);
            var cmd = MakeCommand();
            await i.CommandFailedAsync(cmd, Failed(cmd, new InvalidOperationException("boom")));
            Assert.Equal(LogLevel.Error, Assert.Single(logger.Entries).Level);
        }

        [Fact]
        public async Task Integration_RealContext_LogsExecutedAndFailedCommands()
        {
            var logger = new ListLogger<SchoolInterceptorLogging>();
            var interceptor = new SchoolInterceptorLogging(logger);
            using var conn = TestDb.OpenSqlite();
            using var ctx = TestDb.CreateSqlite(conn, interceptor);
            await ctx.Database.EnsureCreatedAsync();
            await ctx.Database.ExecuteSqlRawAsync("SELECT 1");
            ctx.Database.ExecuteSqlRaw("SELECT 1");
            Assert.NotEmpty(logger.Entries.Where(e => e.Level == LogLevel.Information));

            await Assert.ThrowsAnyAsync<Exception>(() => ctx.Database.ExecuteSqlRawAsync("THIS IS NOT SQL"));
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
        }
    }
}
