using ContosoUniversity.DAL;
using ContosoUniversity.Security;
using System.Data.Entity.Infrastructure.Interception;

namespace ContosoUniversity
{
    /// <summary>
    /// Host-agnostic application start-up logic. Replaces the IIS-bound
    /// System.Web.HttpApplication (Global.asax) so the app can run on Kestrel.
    /// Invoked from Program/Startup (ASP.NET Core host).
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

                DbInterception.Add(new SchoolInterceptorTransientErrors());
                DbInterception.Add(new SchoolInterceptorLogging());
                // Load the HTML sanitization policy (Azure Key Vault) eagerly at start-up.
                HtmlSanitization.Initialize();
                _initialized = true;
            }
        }
    }
}
