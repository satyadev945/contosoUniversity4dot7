using ContosoUniversity.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ContosoUniversity
{
    /// <summary>
    /// Kestrel-hosted ASP.NET Core pipeline replacing the IIS Global.asax / HttpApplication
    /// and the Web.config handler configuration.
    /// </summary>
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            MvcApplication.Application_Start();

            // Content-Security-Policy and related security headers on every response
            app.UseSecurityHeaders();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Replaces HandleErrorAttribute global filter
                app.UseExceptionHandler("/Home/Error");
            }

            // Replaces the IIS BlockViewHandler declared in Views/Web.config
            app.UseBlockViewHandler();

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
