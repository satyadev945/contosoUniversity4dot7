using ContosoUniversity.DAL;
using ContosoUniversity.Middleware;
using ContosoUniversity.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.Entity.Infrastructure.Interception;

namespace ContosoUniversity
{
    /// <summary>
    /// ASP.NET Core startup replacing Global.asax / Application_Start (System.Web.HttpApplication).
    /// Hosted on Kestrel (Cloud Run / GKE); no IIS modules/handlers required.
    ///
    /// Initialization model (cr-dotnet-1033):
    ///  - Centralized configuration (sanitizer policy, etc.) is read from GCP Secret Manager
    ///    (see SanitizationPolicyProvider), so every scaled instance gets identical settings.
    ///  - Only stateless, per-process wiring (EF interceptors, middleware) happens here; it is
    ///    safe to run on every instance.
    ///  - One-time/coordinated work (database migrations, seeding) must NOT run here; execute it
    ///    once per release from a Cloud Run job / init container before instances start.
    /// </summary>
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // HtmlSanitizer (Ganss.Xss) with policy loaded from GCP Secret Manager
            var sanitizer = new HtmlSanitizerService(SanitizationPolicyProvider.Load());
            HtmlSanitization.Initialize(sanitizer);
            services.AddSingleton<IHtmlSanitizerService>(sanitizer);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Formerly Application_Start: stateless per-process wiring only (no shared-state initialization)
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            DbInterception.Add(new SchoolInterceptorLogging());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Replaces HandleErrorAttribute global filter
                app.UseExceptionHandler("/Home/Error");
            }

            // Replaces IIS BlockViewHandler from Views/Web.config
            app.UseMiddleware<BlockViewAccessMiddleware>();

            // Replaces Web Forms HttpModule / web.config customHeaders for CSP (SOC2);
            // paired with a GCP Cloud Armor security policy at the load balancer.
            app.UseMiddleware<ContentSecurityPolicyMiddleware>();

            // Replaces BundleConfig / static content served by IIS
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
