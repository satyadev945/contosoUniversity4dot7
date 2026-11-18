using System;
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

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            // Kestrel listens on the port provided by the platform (PORT env var), default 8080.
            var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<MvcApplication>();
                    webBuilder.UseUrls($"http://0.0.0.0:{port}");
                });
        }
    }
}
