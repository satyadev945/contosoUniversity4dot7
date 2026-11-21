using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace ContosoUniversity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // Kestrel host; port configured via ASPNETCORE_URLS environment variable
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Centralized configuration shared by all scaled instances (replaces per-instance
                    // Global.asax Application_Start settings).
                    var connectionString = Environment.GetEnvironmentVariable("AZURE_APPCONFIG_CONNECTION_STRING");
                    var endpoint = Environment.GetEnvironmentVariable("AZURE_APPCONFIG_ENDPOINT");

                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        config.AddAzureAppConfiguration(connectionString);
                    }
                    else if (!string.IsNullOrWhiteSpace(endpoint))
                    {
                        config.AddAzureAppConfiguration(options =>
                            options.Connect(new Uri(endpoint), new DefaultAzureCredential()));
                    }
                })
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}
