using System;
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

        // Kestrel host; listening port is configured via ASPNETCORE_URLS / PORT environment variables.
        // Configuration is externalized (replaces Web.Debug.config / Web.Release.config transformations):
        //   - Environment variables (highest precedence, injected at runtime)
        //   - AWS Systems Manager Parameter Store, when AWS_SSM_PARAMETER_PATH is set (e.g. /contoso/prod/)
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    var ssmPath = Environment.GetEnvironmentVariable("AWS_SSM_PARAMETER_PATH");
                    if (!string.IsNullOrWhiteSpace(ssmPath))
                    {
                        config.AddSystemsManager(ssmPath);
                    }

                    // Re-add environment variables last so they override Parameter Store values.
                    config.AddEnvironmentVariables();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
