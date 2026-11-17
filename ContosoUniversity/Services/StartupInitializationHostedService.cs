using System.Data.Entity.Infrastructure.Interception;
using System.Threading;
using System.Threading.Tasks;
using ContosoUniversity.DAL;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Replaces Global.asax Application_Start. Runs once per application instance on host start,
    /// using only configuration supplied by Azure App Configuration / environment variables, so every
    /// scaled App Service instance is initialised identically and idempotently.
    /// </summary>
    public class StartupInitializationHostedService : IHostedService
    {
        private static readonly object InitLock = new object();
        private static bool _initialized;

        private readonly ILogger<StartupInitializationHostedService> _logger;

        public StartupInitializationHostedService(ILogger<StartupInitializationHostedService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            lock (InitLock)
            {
                if (_initialized)
                {
                    return Task.CompletedTask;
                }

                // Formerly in Application_Start: EF6 interceptors (registered once per process).
                DbInterception.Add(new SchoolInterceptorTransientErrors());
                DbInterception.Add(new SchoolInterceptorLogging());
                _initialized = true;
            }

            _logger.LogInformation("Application initialization completed (EF interceptors registered).");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
