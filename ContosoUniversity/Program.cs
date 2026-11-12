using ContosoUniversity.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ContosoUniversity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // Kestrel host; port is configurable via ASPNETCORE_URLS (e.g. http://+:8080 on ECS/EKS)
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                // Centralized configuration from AWS Systems Manager Parameter Store (no-op if unset)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddAwsParameterStore(System.Environment.GetEnvironmentVariable("APP_PARAMETER_PATH"));
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
