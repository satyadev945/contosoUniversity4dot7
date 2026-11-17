using System;
using System.Threading;
using System.Threading.Tasks;
using ContosoUniversity.DAL;
using System.Data.Entity.Infrastructure.Interception;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity
{
    /// <summary>
    /// Replaces the Global.asax Application_Start handler. Initialization runs as an ASP.NET Core
    /// IHostedService, so every scaled-out Azure App Service instance executes the same startup
    /// logic before serving traffic. Environment-specific settings are not hard-wired here; they are
    /// supplied centrally through Azure App Configuration (see Program.cs).
    /// HTTP pipeline concerns (routing, static files, compression, error handling) live in Startup.cs.
    /// </summary>
    public class ApplicationInitializationService : IHostedService
    {
        private static readonly object InitLock = new object();
        private static bool _initialized;
        private readonly ILogger<ApplicationInitializationService> _logger;

        public ApplicationInitializationService(ILogger<ApplicationInitializationService> logger)
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

                // EF6 data-access interceptors (process-wide; registered once per instance)
                DbInterception.Add(new SchoolInterceptorTransientErrors());
                DbInterception.Add(new SchoolInterceptorLogging());
                _initialized = true;
            }

            _logger.LogInformation("Application initialization completed on instance {Instance}",
                Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? Environment.MachineName);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
