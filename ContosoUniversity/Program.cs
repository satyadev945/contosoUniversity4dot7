using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ContosoUniversity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Cloud Run init container / job: run one-time initialization (migrations) once, then exit.
            if (System.Array.IndexOf(args, "--init") >= 0)
            {
                MvcApplication.RunOneTimeInitialization();
                return;
            }

            CreateHostBuilder(args).Build().Run();
        }

        // Kestrel host; listens on the PORT/ASPNETCORE_URLS environment configuration (no IIS required).
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
