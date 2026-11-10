using System.Threading;
using System.Threading.Tasks;

namespace ContosoUniversity.Hosting
{
    /// <summary>
    /// Host-agnostic startup/shutdown contract (same shape as
    /// Microsoft.Extensions.Hosting.IHostedService) so that application initialization is
    /// expressed as a service instead of the IIS-bound Global.asax Application_Start event.
    /// </summary>
    public interface IHostedService
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
