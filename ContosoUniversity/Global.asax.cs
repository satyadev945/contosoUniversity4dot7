using ContosoUniversity.DAL;
using ContosoUniversity.Infrastructure;
using System.Data.Entity.Infrastructure.Interception;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

[assembly: System.Web.PreApplicationStartMethod(typeof(ContosoUniversity.MvcApplication), "Application_Start")]

namespace ContosoUniversity
{
    /// <summary>
    /// Application bootstrapper. No longer derives from System.Web.HttpApplication (IIS-hosted
    /// Global.asax); startup logic is a plain static entry point that maps directly to the
    /// ASP.NET Core Program/Startup pipeline (Kestrel) used on Amazon ECS/EKS.
    ///
    /// Configuration is centralised in AWS Systems Manager Parameter Store, and any
    /// fleet-wide one-time initialization is coordinated through a DynamoDB distributed lock
    /// (see <see cref="DistributedInitializer"/>) so every scaled instance ends up consistent.
    /// </summary>
    public static class MvcApplication
    {
        public static void Application_Start()
        {
            // 1. Centralised configuration (identical for all instances).
            DistributedInitializer.LoadConfiguration();

            // 2. Per-process registrations (required on every instance; stateless and idempotent).
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // 3. Fleet-wide one-time initialization guarded by the DynamoDB distributed lock
            //    so it runs once regardless of how many instances start concurrently.
            DistributedInitializer.RunOnce("database-interceptors-warmup", () => { });

            DbInterception.Add(new SchoolInterceptorTransientErrors());
            DbInterception.Add(new SchoolInterceptorLogging());
        }
    }
}
