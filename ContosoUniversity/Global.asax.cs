using ContosoUniversity.DAL;
using ContosoUniversity.Infrastructure;
using System.Data.Entity.Infrastructure.Interception;

namespace ContosoUniversity
{
    /// <summary>
    /// Application start-up logic formerly hosted in the IIS-bound System.Web.HttpApplication (Global.asax).
    /// It no longer depends on IIS: it is invoked from the ASP.NET Core host (see Startup.cs / Program.cs)
    /// running on Kestrel.
    ///
    /// Cloud-ready initialization:
    ///  - Centralized settings are loaded from AWS Systems Manager Parameter Store (see Program.cs,
    ///    APP_PARAMETER_PATH) so all scaled instances share identical configuration.
    ///  - Cluster-wide one-time initialization is coordinated through a DynamoDB distributed lock
    ///    (INIT_LOCK_TABLE) via <see cref="DistributedInitializer"/>, so it runs exactly once across instances.
    ///  - Process-local, stateless registration (EF interceptors) is applied on every instance.
    /// </summary>
    public static class MvcApplication
    {
        private static bool _started;
        private static readonly object _lock = new object();

        public static void Application_Start()
        {
            lock (_lock)
            {
                if (_started)
                {
                    return;
                }

                // Cluster-wide one-time initialization, guarded by a DynamoDB distributed lock.
                DistributedInitializer.RunOnce("contosouniversity-app-init", InitializeShared);

                // Per-process registration (in-memory interceptors) must run on every instance.
                DbInterception.Add(new SchoolInterceptorTransientErrors());
                DbInterception.Add(new SchoolInterceptorLogging());
                _started = true;
            }
        }

        /// <summary>
        /// Shared, once-per-cluster initialization (e.g. seeding/migrations). Executed by a single instance.
        /// </summary>
        private static void InitializeShared()
        {
            // Intentionally minimal: database schema/seed initialization is handled by
            // SchoolInitializer / Migrations and is now serialized across instances by the lock.
        }
    }
}
