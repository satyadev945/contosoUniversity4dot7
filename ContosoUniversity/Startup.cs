using System;
using ContosoUniversity.DAL;
using ContosoUniversity.Infrastructure;
using ContosoUniversity.Middleware;
using ContosoUniversity.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.Entity.Infrastructure.Interception;

namespace ContosoUniversity
{
    /// <summary>
    /// Replaces the IIS-hosted Global.asax / System.Web.HttpApplication lifecycle.
    /// Runs on Kestrel and uses the ASP.NET Core middleware pipeline.
    /// </summary>
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // Central HTML sanitizer (Ganss.Xss); allow-list loaded from AWS Secrets Manager
            services.AddSingleton<IHtmlSanitizerService>(_ => HtmlSanitizerService.Instance);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Cluster-wide initialization (formerly instance-specific Global.asax Application_Start):
            // configuration is read from AWS Systems Manager Parameter Store and one-time setup is
            // coordinated across scaled instances via a DynamoDB lock (see DistributedInitializer).
            DistributedInitializer.Initialize("contoso-university-startup", () =>
            {
                // Place cluster-wide, run-once startup work here (e.g. seeding/migrations).
            });

            // Per-process database interceptors (must be registered on every instance)
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            DbInterception.Add(new SchoolInterceptorLogging());

            // Honour X-Forwarded-* headers from the AWS load balancer (ALB/NLB)
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Replaces HandleErrorAttribute global filter
                app.UseExceptionHandler("/Home/Error");
            }

            // Content-Security-Policy and security headers (Core equivalent of ContentSecurityPolicyModule)
            app.UseMiddleware<ContentSecurityPolicyMiddleware>();

            // Replaces the IIS BlockViewHandler from Views/Web.config
            app.UseMiddleware<BlockViewRequestsMiddleware>();

            // Replaces bundling/static content served by IIS
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                // Replaces RouteConfig.RegisterRoutes
                endpoints.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
