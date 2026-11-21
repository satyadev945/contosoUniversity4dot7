using ContosoUniversity.DAL;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Entity.Infrastructure.Interception;
using System.Threading;
using System.Threading.Tasks;

namespace ContosoUniversity.Hosting
{
    /// <summary>
    /// Replaces Global.asax Application_Start. Runs once per host start on every scaled
    /// Azure App Service instance, performs only stateless/idempotent initialization, and takes
    /// all environment-specific settings from configuration (Azure App Configuration when
    /// AZURE_APPCONFIG_ENDPOINT or AZURE_APPCONFIG_CONNECTION_STRING is set), so every instance
    /// is initialized identically.
    /// </summary>
    public class ApplicationInitializationHostedService : IHostedService
    {
        private static int _interceptorsRegistered;
        private readonly ILogger<ApplicationInitializationHostedService> _logger;

        public ApplicationInitializationHostedService(ILogger<ApplicationInitializationHostedService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Application initialization started.");

            // Guard so the (process-wide) EF6 interceptors are only registered once.
            if (Interlocked.Exchange(ref _interceptorsRegistered, 1) == 0)
            {
                DbInterception.Add(new SchoolInterceptorTransientErrors());
                DbInterception.Add(new SchoolInterceptorLogging());
            }

            _logger.LogInformation("Application initialization completed.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
