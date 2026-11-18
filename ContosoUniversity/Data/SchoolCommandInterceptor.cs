using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// Logs executed SQL commands with their duration and any failures.
    /// Replaces the EF6 SchoolInterceptorLogging class. Timing comes from the EF Core event data,
    /// so no shared (non thread-safe) Stopwatch is required.
    /// </summary>
    public class SchoolCommandInterceptor : DbCommandInterceptor
    {
        private readonly ILogger<SchoolCommandInterceptor> _logger;

        public SchoolCommandInterceptor(ILogger<SchoolCommandInterceptor> logger)
        {
            _logger = logger;
        }

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            LogExecuted("ReaderExecuted", command, eventData.Duration);
            return base.ReaderExecuted(command, eventData, result);
        }

        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            LogExecuted("ScalarExecuted", command, eventData.Duration);
            return base.ScalarExecuted(command, eventData, result);
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            LogExecuted("NonQueryExecuted", command, eventData.Duration);
            return base.NonQueryExecuted(command, eventData, result);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            LogExecuted("ReaderExecutedAsync", command, eventData.Duration);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
        {
            LogExecuted("ScalarExecutedAsync", command, eventData.Duration);
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            LogExecuted("NonQueryExecutedAsync", command, eventData.Duration);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
            _logger.LogError(eventData.Exception, "Error executing command: {CommandText}", command.CommandText);
            base.CommandFailed(command, eventData);
        }

        public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            _logger.LogError(eventData.Exception, "Error executing command: {CommandText}", command.CommandText);
            return base.CommandFailedAsync(command, eventData, cancellationToken);
        }

        private void LogExecuted(string operation, DbCommand command, TimeSpan duration)
        {
            _logger.LogInformation("SQL Database {Operation} completed in {ElapsedMs} ms. Command: {CommandText}",
                operation, duration.TotalMilliseconds, command.CommandText);
        }
    }
}
