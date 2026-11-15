using System.Data.Common;
using ContosoUniversity.Logging;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ContosoUniversity.DAL
{
    public class SchoolInterceptorLogging : DbCommandInterceptor
    {
        private readonly ILogger _logger = new Logger();

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

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            _logger.TraceApi("SQL Database", "SchoolInterceptor.ReaderExecuted", eventData.Duration, "Command: {0}: ", command.CommandText);
            return base.ReaderExecuted(command, eventData, result);
        }

        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
            _logger.Error(eventData.Exception, "Error executing command: {0}", command.CommandText);
            base.CommandFailed(command, eventData);
        }
    }
}
