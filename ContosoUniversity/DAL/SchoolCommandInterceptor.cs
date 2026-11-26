using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ContosoUniversity.DAL
{
    /// <summary>
    /// Logs the duration of every executed SQL command and any command failures.
    /// Replaces the EF6 SchoolInterceptorLogging class. Durations come from the event data,
    /// so no shared (non thread-safe) Stopwatch is needed.
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
            LogExecuted("ReaderExecuted", command, eventData);
            return base.ReaderExecuted(command, eventData, result);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            LogExecuted("ReaderExecuted", command, eventData);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            LogExecuted("NonQueryExecuted", command, eventData);
            return base.NonQueryExecuted(command, eventData, result);
        }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            LogExecuted("NonQueryExecuted", command, eventData);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            LogExecuted("ScalarExecuted", command, eventData);
            return base.ScalarExecuted(command, eventData, result);
        }

        public override ValueTask<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
        {
            LogExecuted("ScalarExecuted", command, eventData);
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
            LogFailed(command, eventData);
            base.CommandFailed(command, eventData);
        }

        public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            LogFailed(command, eventData);
            return base.CommandFailedAsync(command, eventData, cancellationToken);
        }

        private void LogExecuted(string method, DbCommand command, CommandExecutedEventData eventData)
        {
            _logger.LogInformation(
                "SQL Database {Method} took {ElapsedMilliseconds} ms. Command: {CommandText}",
                method,
                eventData.Duration.TotalMilliseconds,
                command.CommandText);
        }

        private void LogFailed(DbCommand command, CommandErrorEventData eventData)
        {
            _logger.LogError(
                eventData.Exception,
                "Error executing command: {CommandText}",
                command.CommandText);
        }
    }
}
