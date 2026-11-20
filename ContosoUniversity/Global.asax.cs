using ContosoUniversity.DAL;
using ContosoUniversity.Infrastructure;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace ContosoUniversity
{
    /// <summary>
    /// Host-agnostic application startup. This logic was moved out of the IIS-bound
    /// System.Web.HttpApplication so it can be invoked from an ASP.NET Core
    /// Program/Startup (Kestrel) pipeline, e.g. from Startup.Configure().
    /// </summary>
    public static class ApplicationStartup
    {
        private static readonly object SyncRoot = new object();
        private static bool _initialized;

        public static void Initialize()
        {
            lock (SyncRoot)
            {
                if (_initialized)
                {
                    return;
                }

                // Centralized configuration: every instance reads the same AWS Systems Manager Parameter Store values.
                ParameterStoreSettings.Load();

                // Per-process registrations (stateless, identical on every instance).
                AreaRegistration.RegisterAllAreas();
                FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                BundleConfig.RegisterBundles(BundleTable.Bundles);
                DbInterception.Add(new SchoolInterceptorTransientErrors());
                DbInterception.Add(new SchoolInterceptorLogging());

                // Cluster-wide one-time initialization (database initialization/seeding): guarded by a
                // DynamoDB distributed lock so it runs once across all scaled instances, not once per instance.
                DistributedInitializer.RunOnce("ContosoUniversity:database-init", () =>
                {
                    using (var context = new SchoolContext())
                    {
                        context.Database.Initialize(false);
                    }
                });
                _initialized = true;
            }
        }
    }

    /// <summary>
    /// Transitional shim for the legacy IIS host (Global.asax). Contains no instance-specific logic of its own;
    /// all initialization is delegated to ApplicationStartup (Parameter Store + DynamoDB distributed lock);
    /// remove together with Global.asax once the ASP.NET Core host calls ApplicationStartup.Initialize().
    /// </summary>
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            ApplicationStartup.Initialize();
        }
    }
}
