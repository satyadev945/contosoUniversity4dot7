using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ContosoUniversity.DAL
{
    /// <summary>
    /// Logs the duration and outcome of every database command using ILogger.
    /// </summary>
    public class SchoolInterceptorLogging : DbCommandInterceptor
    {
        private readonly ILogger<SchoolInterceptorLogging> _logger;

        public SchoolInterceptorLogging(ILogger<SchoolInterceptorLogging> logger)
        {
            _logger = logger;
        }

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            LogExecuted("ReaderExecuted", eventData);
            return base.ReaderExecuted(command, eventData, result);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            LogExecuted("ReaderExecutedAsync", eventData);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            LogExecuted("NonQueryExecuted", eventData);
            return base.NonQueryExecuted(command, eventData, result);
        }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            LogExecuted("NonQueryExecutedAsync", eventData);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            LogExecuted("ScalarExecuted", eventData);
            return base.ScalarExecuted(command, eventData, result);
        }

        public override ValueTask<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
        {
            LogExecuted("ScalarExecutedAsync", eventData);
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
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

        private void LogExecuted(string method, CommandExecutedEventData eventData)
        {
            _logger.LogDebug("SQL Database {Method} took {Elapsed} ms. Command: {CommandText}",
                method, eventData.Duration.TotalMilliseconds, eventData.Command.CommandText);
        }
    }
}
