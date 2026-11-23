using System.Threading;
using System.Threading.Tasks;

namespace ContosoUniversity.Hosting
{
    /// <summary>
    /// Mirrors the Microsoft.Extensions.Hosting.IHostedService contract so that startup/shutdown
    /// work can be expressed as hosted services on .NET Framework 4.7 and moved unchanged to
    /// ASP.NET Core (services.AddHostedService&lt;T&gt;()) when the host is migrated.
    /// </summary>
    public interface IHostedService
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
