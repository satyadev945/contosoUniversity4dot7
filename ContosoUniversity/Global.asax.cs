using System.Web;
using ContosoUniversity.Hosting;

// Host-agnostic startup hook: replaces the IIS-specific Global.asax / HttpApplication
// (Application_Start). Initialization is performed by StartupHostedService (IHostedService),
// which first loads centralized settings from Azure App Configuration so that all scaled
// instances are initialized consistently.
[assembly: PreApplicationStartMethod(typeof(ContosoUniversity.ApplicationStartup), "Start")]

namespace ContosoUniversity
{
    public static class ApplicationStartup
    {
        public static void Start()
        {
            HostedServiceRunner.Run(new StartupHostedService());
        }
    }
}
