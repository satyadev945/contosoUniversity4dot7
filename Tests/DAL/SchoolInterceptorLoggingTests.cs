using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ContosoUniversity.DAL
{
    public class SchoolInterceptorLoggingTests
    {
        private static (TestDb db, CapturingLogger<SchoolInterceptorLogging> logger) Create()
        {
            var logger = new CapturingLogger<SchoolInterceptorLogging>();
            var db = new TestDb(true, new SchoolInterceptorLogging(logger));
            return (db, logger);
        }

        [Fact]
        public void Constructor_AcceptsLogger()
        {
            var interceptor = new SchoolInterceptorLogging(new CapturingLogger<SchoolInterceptorLogging>());
            Assert.NotNull(interceptor);
        }

        [Fact]
        public void ReaderExecuted_Sync_LogsInformation()
        {
            var (db, logger) = Create();
            using (db)
            {
                logger.Entries.Clear();
                using var ctx = db.CreateContext();
                _ = ctx.Students.ToList();
                Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("SchoolInterceptor.ReaderExecuted"));
            }
        }

        [Fact]
        public async Task ReaderExecuted_Async_LogsInformation()
        {
            var (db, logger) = Create();
            using (db)
            {
                logger.Entries.Clear();
                using var ctx = db.CreateContext();
                _ = await ctx.Students.ToListAsync();
                Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("SchoolInterceptor.ReaderExecuted"));
            }
        }

        [Fact]
        public void NonQueryExecuted_Sync_LogsInformation()
        {
            var (db, logger) = Create();
            using (db)
            {
                logger.Entries.Clear();
                using var ctx = db.CreateContext();
                ctx.Database.ExecuteSqlRaw("DELETE FROM Student");
                Assert.Contains(logger.Entries, e => e.Message.Contains("SchoolInterceptor.NonQueryExecuted"));
            }
        }

        [Fact]
        public async Task NonQueryExecuted_Async_LogsInformation()
        {
            var (db, logger) = Create();
            using (db)
            {
                logger.Entries.Clear();
                using var ctx = db.CreateContext();
                await ctx.Database.ExecuteSqlRawAsync("DELETE FROM Student");
                Assert.Contains(logger.Entries, e => e.Message.Contains("SchoolInterceptor.NonQueryExecuted"));
            }
        }

        [Fact]
        public void ScalarExecuted_Sync_LogsInformation()
        {
            var logger = new CapturingLogger<SchoolInterceptorLogging>();
            using var db = new TestDb(false, new SchoolInterceptorLogging(logger));
            using var ctx = db.CreateContext();
            // SQLite's database creator issues a scalar command to check for existing tables.
            ctx.Database.EnsureCreated();
            Assert.Contains(logger.Entries, e => e.Message.Contains("SchoolInterceptor.ScalarExecuted"));
        }

        [Fact]
        public async Task ScalarExecuted_Async_LogsInformation()
        {
            var logger = new CapturingLogger<SchoolInterceptorLogging>();
            using var db = new TestDb(false, new SchoolInterceptorLogging(logger));
            using var ctx = db.CreateContext();
            await ctx.Database.EnsureCreatedAsync();
            Assert.Contains(logger.Entries, e => e.Message.Contains("SchoolInterceptor.ScalarExecuted"));
        }

        [Fact]
        public void CommandFailed_Sync_LogsError()
        {
            var (db, logger) = Create();
            using (db)
            {
                using var ctx = db.CreateContext();
                Assert.ThrowsAny<Exception>(() => ctx.Database.ExecuteSqlRaw("SELECT * FROM DoesNotExist"));
                var error = Assert.Single(logger.Entries, e => e.Level == LogLevel.Error);
                Assert.Contains("DoesNotExist", error.Message);
                Assert.NotNull(error.Exception);
            }
        }

        [Fact]
        public async Task CommandFailed_Async_LogsError()
        {
            var (db, logger) = Create();
            using (db)
            {
                using var ctx = db.CreateContext();
                await Assert.ThrowsAnyAsync<Exception>(() => ctx.Database.ExecuteSqlRawAsync("SELECT * FROM DoesNotExist"));
                Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("DoesNotExist"));
            }
        }
    }
}
