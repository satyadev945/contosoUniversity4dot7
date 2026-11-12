using System;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ContosoUniversity.Logging;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ContosoUniversity.DAL
{
    public class SchoolInterceptorLogging : DbCommandInterceptor
    {
        private readonly ILogger _logger = new Logger();

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            _logger.TraceApi("SQL Database", "SchoolInterceptor.ReaderExecuted", eventData.Duration, "Command: {0}: ", command.CommandText);
            return base.ReaderExecuted(command, eventData, result);
        }

        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            _logger.TraceApi("SQL Database", "SchoolInterceptor.ScalarExecuted", eventData.Duration, "Command: {0}: ", command.CommandText);
            return base.ScalarExecuted(command, eventData, result);
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            _logger.TraceApi("SQL Database", "SchoolInterceptor.NonQueryExecuted", eventData.Duration, "Command: {0}: ", command.CommandText);
            return base.NonQueryExecuted(command, eventData, result);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            _logger.TraceApi("SQL Database", "SchoolInterceptor.ReaderExecuted", eventData.Duration, "Command: {0}: ", command.CommandText);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
            _logger.Error(eventData.Exception, "Error executing command: {0}", command.CommandText);
            base.CommandFailed(command, eventData);
        }

        public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            _logger.Error(eventData.Exception, "Error executing command: {0}", command.CommandText);
            return base.CommandFailedAsync(command, eventData, cancellationToken);
        }
    }
}
