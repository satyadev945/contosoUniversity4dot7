using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ContosoUniversity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // Kestrel host; listening URLs come from ASPNETCORE_URLS / PORT environment (no IIS dependency).
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Centralized, consistent configuration across all scaled Azure App Service instances.
                    // Endpoint is supplied via environment (AZURE_APPCONFIG_ENDPOINT) or connection string
                    // (AZURE_APPCONFIG_CONNECTION_STRING); both are optional for local development.
                    var connectionString = System.Environment.GetEnvironmentVariable("AZURE_APPCONFIG_CONNECTION_STRING");
                    var endpoint = System.Environment.GetEnvironmentVariable("AZURE_APPCONFIG_ENDPOINT");

                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        config.AddAzureAppConfiguration(o => o.Connect(connectionString));
                    }
                    else if (!string.IsNullOrEmpty(endpoint))
                    {
                        config.AddAzureAppConfiguration(o =>
                            o.Connect(new System.Uri(endpoint), new Azure.Identity.DefaultAzureCredential()));
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel();
                    webBuilder.UseStartup<Startup>();
                });
    }
}
