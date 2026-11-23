using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

[assembly: PreApplicationStartMethod(typeof(ContosoUniversity.Hosting.HostedServiceRunner), "Start")]

namespace ContosoUniversity.Hosting
{
    /// <summary>
    /// Starts all registered IHostedService instances during pre-application start (no Global.asax)
    /// and stops them gracefully when the app domain shuts down (e.g. Azure App Service scale-in).
    /// </summary>
    public static class HostedServiceRunner
    {
        private static readonly List<IHostedService> Services = new List<IHostedService>
        {
            new ApplicationStartupHostedService()
        };

        public static void Start()
        {
            foreach (var service in Services)
            {
                service.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            HostingEnvironment.RegisterObject(new ShutdownHook());
        }

        private sealed class ShutdownHook : IRegisteredObject
        {
            public void Stop(bool immediate)
            {
                try
                {
                    foreach (var service in Services)
                    {
                        service.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
                    }
                }
                finally
                {
                    HostingEnvironment.UnregisterObject(this);
                }
            }
        }
    }
}
