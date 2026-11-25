using System;
using System.Linq;
using System.Threading.Tasks;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ContosoUniversity.Data
{
    public class SchoolCommandLoggingInterceptorTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ListLogger<SchoolCommandLoggingInterceptor> _logger = new();
        private readonly SchoolContext _context;

        public SchoolCommandLoggingInterceptorTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseSqlite(_connection)
                .AddInterceptors(new SchoolCommandLoggingInterceptor(_logger))
                .Options;
            _context = new SchoolContext(options);
            _context.Database.EnsureCreated();
            _logger.Entries.Clear();
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public void Constructor_AcceptsLogger()
        {
            Assert.NotNull(new SchoolCommandLoggingInterceptor(_logger));
        }

        [Fact]
        public void ReaderExecuted_LogsInformationWithCommandText()
        {
            _ = _context.Students.ToList();

            var entry = Assert.Single(_logger.Entries, e => e.Level == LogLevel.Information);
            Assert.Contains("Database command executed", entry.Message);
            Assert.Contains("Student", entry.Message);
        }

        [Fact]
        public async Task ReaderExecutedAsync_LogsInformation()
        {
            _ = await _context.Students.ToListAsync();

            Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Student"));
        }

        [Fact]
        public void NonQueryExecuted_LogsInformation()
        {
            _context.Database.ExecuteSqlRaw("DELETE FROM Student");

            Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("DELETE FROM Student"));
        }

        [Fact]
        public async Task NonQueryExecutedAsync_LogsInformation()
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Student");

            Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("DELETE FROM Student"));
        }

        [Fact]
        public void SaveChanges_LogsInsertCommand()
        {
            _context.Students.Add(new Student { FirstMidName = "A", LastName = "B", EnrollmentDate = DateTime.Today });
            _context.SaveChanges();

            Assert.Contains(_logger.Entries, e => e.Message.Contains("INSERT"));
        }

        [Fact]
        public void CommandFailed_LogsError()
        {
            Assert.ThrowsAny<Exception>(() => _context.Database.ExecuteSqlRaw("SELECT * FROM DoesNotExist"));

            var entry = Assert.Single(_logger.Entries, e => e.Level == LogLevel.Error);
            Assert.Contains("Database command failed", entry.Message);
            Assert.NotNull(entry.Exception);
        }

        [Fact]
        public async Task CommandFailedAsync_LogsError()
        {
            await Assert.ThrowsAnyAsync<Exception>(() => _context.Database.ExecuteSqlRawAsync("SELECT * FROM DoesNotExist"));

            Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Error && e.Exception != null);
        }

        [Fact]
        public void ReaderFailure_LogsError()
        {
            Assert.ThrowsAny<Exception>(() => _context.Students.FromSqlRaw("SELECT * FROM DoesNotExist").ToList());

            Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Error);
        }
    }
}
