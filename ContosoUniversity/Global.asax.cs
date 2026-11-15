using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Entity.Infrastructure.Interception;
using System.Threading;
using System.Threading.Tasks;

namespace ContosoUniversity
{
    /// <summary>
    /// Replaces the IIS-hosted System.Web.HttpApplication (Global.asax). Application startup and the
    /// request pipeline are now expressed as ASP.NET Core middleware that runs on Kestrel.
    /// </summary>
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            // Replaces Global.asax Application_Start: runs identically on every scaled instance at host startup
            services.AddHostedService<ApplicationStartupService>();
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();

            // Kestrel equivalent of ContentSecurityPolicyModule: emit CSP and companion security headers
            app.Use(async (context, next) =>
            {
                var csp = System.Environment.GetEnvironmentVariable("CONTENT_SECURITY_POLICY");
                context.Response.Headers["Content-Security-Policy"] =
                    string.IsNullOrWhiteSpace(csp) ? Security.ContentSecurityPolicyModule.DefaultPolicy : csp;
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                await next();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Replaces HandleErrorAttribute / IIS custom errors
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseResponseCompression();   // replaces IIS dynamic/static compression module
            app.UseStaticFiles();           // replaces IIS static file handler (serves wwwroot only)
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }

    /// <summary>
    /// Replaces Global.asax Application_Start. Executes once per host instance, driven only by
    /// centralized configuration (Azure App Configuration) so every scaled instance initializes identically.
    /// </summary>
    public class ApplicationStartupService : IHostedService
    {
        private readonly ILogger<ApplicationStartupService> _logger;

        public ApplicationStartupService(ILogger<ApplicationStartupService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Register EF6 interceptors (formerly in Application_Start)
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            DbInterception.Add(new SchoolInterceptorLogging());
            _logger.LogInformation("Application initialization completed (EF6 interceptors registered).");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Centralized configuration shared by all scaled Azure App Service instances
                    var connectionString = Environment.GetEnvironmentVariable("AZURE_APPCONFIG_CONNECTION_STRING");
                    var endpoint = Environment.GetEnvironmentVariable("AZURE_APPCONFIG_ENDPOINT");
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        config.AddAzureAppConfiguration(o => o.Connect(connectionString));
                    }
                    else if (!string.IsNullOrWhiteSpace(endpoint))
                    {
                        config.AddAzureAppConfiguration(o => o.Connect(new Uri(endpoint), new DefaultAzureCredential()));
                    }
                })
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
                .Build()
                .Run();
        }
    }
}
