using ContosoUniversity.Hosting;
using ContosoUniversity.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ContosoUniversity
{
    /// <summary>
    /// ASP.NET Core startup replacing the IIS/System.Web HttpApplication (Global.asax) pipeline.
    /// Runs on Kestrel; no IIS modules/handlers are required.
    /// </summary>
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Replaces IIS dynamic compression module
            services.AddResponseCompression(options => options.EnableForHttps = true);

            // Replaces AreaRegistration / FilterConfig / RouteConfig
            services.AddControllersWithViews();

            // Replaces System.Web.Optimization BundleConfig (static files served from wwwroot)

            // Replaces Global.asax Application_Start (EF interceptor registration etc.)
            services.AddHostedService<ApplicationInitializationHostedService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Replaces HandleErrorAttribute global filter
                app.UseExceptionHandler("/Home/Error");
            }

            // Content-Security-Policy and related security headers on every response
            app.UseContentSecurityPolicy();

            app.UseResponseCompression();

            // Replaces IIS static file handler; Views are never exposed (replaces BlockViewHandler)
            app.UseStaticFiles();

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
