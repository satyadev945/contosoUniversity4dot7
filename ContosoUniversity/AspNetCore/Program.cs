using System;
using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ContosoUniversity.AspNetCore
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Centralized, consistent configuration for all scaled instances via Azure App Configuration.
                    // Endpoint is supplied through the AZURE_APPCONFIG_ENDPOINT environment variable
                    // and accessed with Managed Identity (DefaultAzureCredential).
                    var endpoint = Environment.GetEnvironmentVariable("AZURE_APPCONFIG_ENDPOINT");
                    if (!string.IsNullOrWhiteSpace(endpoint))
                    {
                        config.AddAzureAppConfiguration(options =>
                            options.Connect(new Uri(endpoint), new DefaultAzureCredential())
                                   .Select("ContosoUniversity:*")
                                   .ConfigureKeyVault(kv => kv.SetCredential(new DefaultAzureCredential())));
                    }
                })
                .ConfigureWebHostDefaults(web =>
                {
                    // Kestrel; port supplied via ASPNETCORE_URLS / PORT environment variables.
                    web.UseStartup<Startup>();
                })
                .Build()
                .Run();
        }
    }
}
