using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Data.Entity.Infrastructure.Interception;
using ContosoUniversity.DAL;

namespace ContosoUniversity.Hosting
{
    /// <summary>
    /// Host-agnostic application startup. Replaces the Global.asax Application_Start logic.
    /// Every scaled instance runs the same idempotent initialization, driven only by configuration
    /// that is centralized in Azure App Configuration (surfaced through ConfigurationManager.AppSettings
    /// by the configuration builders registered in Web.config) and environment variables.
    /// </summary>
    public class ApplicationStartupHostedService : IHostedService
    {
        private static readonly object SyncRoot = new object();
        private static bool _started;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Configure();
            return Task.FromResult(0);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public static void Configure()
        {
            lock (SyncRoot)
            {
                if (_started) return;
                AreaRegistration.RegisterAllAreas();
                FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                BundleConfig.RegisterBundles(BundleTable.Bundles);
                DbInterception.Add(new SchoolInterceptorTransientErrors());
                DbInterception.Add(new SchoolInterceptorLogging());
                _started = true;
            }
        }
    }
}
