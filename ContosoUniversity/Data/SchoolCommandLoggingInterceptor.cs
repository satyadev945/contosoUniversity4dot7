using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// Logs every executed SQL command together with its duration, and logs failed commands as errors.
    /// Replaces the EF6 SchoolInterceptorLogging class. Duration comes from the event data, so the
    /// timing is correct for concurrent requests (no shared Stopwatch).
    /// </summary>
    public class SchoolCommandLoggingInterceptor : DbCommandInterceptor
    {
        private readonly ILogger<SchoolCommandLoggingInterceptor> _logger;

        public SchoolCommandLoggingInterceptor(ILogger<SchoolCommandLoggingInterceptor> logger)
        {
            _logger = logger;
        }

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            LogExecuted("Reader", command, eventData);
            return base.ReaderExecuted(command, eventData, result);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            LogExecuted("Reader", command, eventData);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            LogExecuted("NonQuery", command, eventData);
            return base.NonQueryExecuted(command, eventData, result);
        }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            LogExecuted("NonQuery", command, eventData);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            LogExecuted("Scalar", command, eventData);
            return base.ScalarExecuted(command, eventData, result);
        }

        public override ValueTask<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
        {
            LogExecuted("Scalar", command, eventData);
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

        private void LogExecuted(string kind, DbCommand command, CommandExecutedEventData eventData)
        {
            _logger.LogDebug(
                "SQL {CommandKind} executed in {ElapsedMilliseconds} ms: {CommandText}",
                kind,
                eventData.Duration.TotalMilliseconds,
                command.CommandText);
        }

        private void LogFailed(DbCommand command, CommandErrorEventData eventData)
        {
            _logger.LogError(
                eventData.Exception,
                "SQL command failed after {ElapsedMilliseconds} ms: {CommandText}",
                eventData.Duration.TotalMilliseconds,
                command.CommandText);
        }
    }
}
