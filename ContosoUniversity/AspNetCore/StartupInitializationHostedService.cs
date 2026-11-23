using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.AspNetCore
{
    /// <summary>
    /// Replaces Global.asax Application_Start. Runs the host-agnostic initialization
    /// on every scaled instance during host start-up, using configuration that is
    /// centrally supplied by Azure App Configuration (see Program.cs) so that all
    /// instances are initialized consistently.
    /// </summary>
    public class StartupInitializationHostedService : IHostedService
    {
        private readonly ILogger<StartupInitializationHostedService> _logger;

        public StartupInitializationHostedService(ILogger<StartupInitializationHostedService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running application start-up initialization (replaces Global.asax Application_Start).");
            ApplicationStartup.Initialize();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
