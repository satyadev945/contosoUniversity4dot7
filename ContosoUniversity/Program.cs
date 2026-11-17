using System;
using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ContosoUniversity
{
    // Kestrel-hosted entry point (no IIS / System.Web dependency).
    // Listening port/URLs are taken from ASPNETCORE_URLS / PORT environment variables.
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Centralized, consistent configuration for all scaled instances via Azure App Configuration.
                    // Enabled when AZURE_APPCONFIG_ENDPOINT is set; authenticates with managed identity.
                    var endpoint = Environment.GetEnvironmentVariable("AZURE_APPCONFIG_ENDPOINT");
                    if (!string.IsNullOrWhiteSpace(endpoint))
                    {
                        config.AddAzureAppConfiguration(options =>
                            options.Connect(new Uri(endpoint), new DefaultAzureCredential())
                                   .Select("ContosoUniversity:*"));
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel();
                    webBuilder.UseStartup<Startup>();
                });
    }
}
