using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// EF Core command interceptor that logs executed SQL commands, their duration and failures.
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
            _logger.LogError(eventData.Exception, "Error executing command: {CommandText}", command.CommandText);
            base.CommandFailed(command, eventData);
        }

        public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            _logger.LogError(eventData.Exception, "Error executing command: {CommandText}", command.CommandText);
            return base.CommandFailedAsync(command, eventData, cancellationToken);
        }

        private void LogExecuted(string operation, DbCommand command, CommandExecutedEventData eventData)
        {
            _logger.LogDebug("SQL Database {Operation} took {ElapsedMs} ms. Command: {CommandText}",
                operation, eventData.Duration.TotalMilliseconds, command.CommandText);
        }
    }
}
