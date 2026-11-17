using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using ContosoUniversity.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ContosoUniversity
{
    /// <summary>
    /// Replaces the IIS-hosted Global.asax / System.Web.HttpApplication.
    /// Pipeline is hosted by Kestrel using ASP.NET Core middleware.
    /// </summary>
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddResponseCompression();

            // Distributed cache: Azure Cache for Redis when configured, in-memory fallback for local dev.
            var redis = System.Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
            if (!string.IsNullOrEmpty(redis))
            {
                services.AddStackExchangeRedisCache(o =>
                {
                    o.Configuration = redis;
                    o.InstanceName = "ContosoUniversity:";
                });
            }
            else
            {
                services.AddDistributedMemoryCache();
            }
            services.AddSingleton<IStudentCache, StudentCache>();

            // Replaces Global.asax Application_Start: one-time initialization via IHostedService
            services.AddHostedService<StartupInitializationHostedService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Replaces FilterConfig HandleErrorAttribute
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // Content-Security-Policy header (Kestrel equivalent of CspHeaderModule / web.config customHeaders)
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
                    {
                        context.Response.Headers["Content-Security-Policy"] = CspHeaderModule.GetPolicy();
                    }
                    return System.Threading.Tasks.Task.CompletedTask;
                });
                await next();
            });

            // Replaces IIS compression module and static file handler
            app.UseResponseCompression();
            app.UseStaticFiles();

            // Replaces RouteConfig / BundleConfig (bundling is handled at build time)
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
