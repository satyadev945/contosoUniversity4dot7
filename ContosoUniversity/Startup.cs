using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.Entity.Infrastructure.Interception;

namespace ContosoUniversity
{
    /// <summary>
    /// Replaces Global.asax / MvcApplication (IIS System.Web.HttpApplication).
    /// Request pipeline is built from ASP.NET Core middleware running on Kestrel.
    /// </summary>
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Replaces AreaRegistration, FilterConfig (global filters) and MVC registration
            services.AddControllersWithViews();
            services.AddHealthChecks();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Honour X-Forwarded-* headers from the AWS load balancer (ALB/NLB)
            app.UseForwardedHeaders();

            // Replaces HandleErrorAttribute global filter / customErrors
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // Content-Security-Policy and companion security headers (Kestrel equivalent of
            // ContosoUniversity.Security.ContentSecurityPolicyModule). Override with CSP_POLICY env var.
            var cspPolicy = System.Environment.GetEnvironmentVariable("CSP_POLICY")
                ?? ContosoUniversity.Security.ContentSecurityPolicyModule.DefaultPolicy;
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    var headers = context.Response.Headers;
                    headers["Content-Security-Policy"] = cspPolicy;
                    headers["X-Content-Type-Options"] = "nosniff";
                    headers["X-Frame-Options"] = "DENY";
                    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                    return System.Threading.Tasks.Task.CompletedTask;
                });
                await next();
            });

            // Replaces BundleConfig / IIS static file handler
            app.UseStaticFiles();

            // Replaces RouteConfig.RegisterRoutes
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            // Replaces Global.asax Application_Start: shared configuration comes from AWS Systems Manager
            // Parameter Store and one-time cluster-wide initialization is coordinated with a DynamoDB lock,
            // so every scaled instance ends up with a consistent setup.
            var initializer = new ContosoUniversity.Infrastructure.DistributedInitializer();
            initializer.Initialize();

            // Per-process EF interceptors must be registered on every instance.
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            string enableSqlLogging;
            if (!initializer.Configuration.TryGetValue("EnableSqlLogging", out enableSqlLogging)
                || !string.Equals(enableSqlLogging, "false", System.StringComparison.OrdinalIgnoreCase))
            {
                DbInterception.Add(new SchoolInterceptorLogging());
            }
        }
    }
}
