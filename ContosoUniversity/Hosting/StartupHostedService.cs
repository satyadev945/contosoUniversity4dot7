using System.Data.Entity.Infrastructure.Interception;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using ContosoUniversity.DAL;

namespace ContosoUniversity.Hosting
{
    /// <summary>
    /// Replaces Global.asax Application_Start. Loads centralized configuration from
    /// Azure App Configuration, then performs the MVC/EF initialization identically on every
    /// scaled instance.
    /// </summary>
    public class StartupHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                AzureAppConfigurationSettings.Load();
            }
            catch (System.Exception ex)
            {
                // Do not prevent the instance from starting; fall back to environment/appSettings.
                Trace.TraceError("Unable to load settings from Azure App Configuration: " + ex.Message);
            }

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            DbInterception.Add(new SchoolInterceptorLogging());

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
