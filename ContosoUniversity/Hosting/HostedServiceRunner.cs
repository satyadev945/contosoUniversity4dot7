using System.Threading;
using System.Web.Hosting;

namespace ContosoUniversity.Hosting
{
    /// <summary>
    /// Starts IHostedService instances and stops them gracefully when the host shuts down
    /// (e.g. App Service instance recycle or scale-in).
    /// </summary>
    public sealed class HostedServiceRunner : IRegisteredObject
    {
        private readonly IHostedService _service;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private HostedServiceRunner(IHostedService service)
        {
            _service = service;
        }

        public static void Run(IHostedService service)
        {
            var runner = new HostedServiceRunner(service);
            service.StartAsync(runner._cts.Token).GetAwaiter().GetResult();
            HostingEnvironment.RegisterObject(runner);
        }

        public void Stop(bool immediate)
        {
            try
            {
                _cts.Cancel();
                _service.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            finally
            {
                HostingEnvironment.UnregisterObject(this);
            }
        }
    }
}
