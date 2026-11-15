using ContosoUniversity.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ContosoUniversity
{
    /// <summary>
    /// ASP.NET Core middleware pipeline replacing Global.asax / IIS modules and handlers.
    /// Runs on Kestrel (no IIS dependency) so it can be hosted on GKE.
    /// </summary>
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            MvcApplication.Initialize();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Replaces FilterConfig's HandleErrorAttribute
                app.UseExceptionHandler("/Home/Error");
            }

            // Content-Security-Policy and related security headers (SOC2); complements GCP Cloud Armor
            app.UseMiddleware<CspHeadersMiddleware>();

            // Replaces the IIS BlockViewHandler from Views/Web.config
            app.UseMiddleware<BlockViewsMiddleware>();

            // Replaces BundleConfig / IIS static file handler
            app.UseStaticFiles();

            app.UseRouting();

            // Replaces RouteConfig.RegisterRoutes
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
