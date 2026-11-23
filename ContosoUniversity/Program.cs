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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Environment-specific configuration is injected at runtime (environment variables,
                    // e.g. ConnectionStrings__DefaultConnection) instead of Web.Debug/Release.config transforms.
                    // Values from AWS Systems Manager Parameter Store can be surfaced as environment variables
                    // (ECS task definition "secrets"/EKS External Secrets) and override file-based settings.
                    config.AddEnvironmentVariables();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // Kestrel listens on the port provided by the container platform (ECS/EKS)
                    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
                    webBuilder.UseUrls($"http://0.0.0.0:{port}");
                    webBuilder.UseStartup<Startup>();
                });
        }
}
