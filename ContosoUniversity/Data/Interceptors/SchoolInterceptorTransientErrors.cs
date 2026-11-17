using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ContosoUniversity.Data.Interceptors
{
    /// <summary>
    /// Development-only helper used to demonstrate connection resiliency. When the first command parameter is
    /// "%Throw%", the next few reader executions fail with a transient error (TimeoutException is treated as
    /// transient by the SQL Server execution strategy) and the parameters are rewritten to "%an%".
    /// </summary>
    public class SchoolInterceptorTransientErrors : DbCommandInterceptor
    {
        private const int MaxSimulatedFailures = 4;
        private int _counter;
        private readonly ILogger<SchoolInterceptorTransientErrors> _logger;

        public SchoolInterceptorTransientErrors(ILogger<SchoolInterceptorTransientErrors> logger)
        {
            _logger = logger;
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            SimulateTransientError(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            SimulateTransientError(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void SimulateTransientError(DbCommand command)
        {
            if (command.Parameters.Count == 0 || command.Parameters[0].Value?.ToString() != "%Throw%")
            {
                return;
            }

            if (Interlocked.Increment(ref _counter) <= MaxSimulatedFailures)
            {
                _logger.LogInformation("Returning transient error for command: {CommandText}", command.CommandText);
                throw new TimeoutException("Simulated transient database error.");
            }

            foreach (DbParameter parameter in command.Parameters)
            {
                parameter.Value = "%an%";
            }
        }
    }
}
