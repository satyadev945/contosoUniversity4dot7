using System;
using System.Diagnostics;
using System.Data.Entity.Infrastructure.Interception;
using ContosoUniversity.Configuration;
using ContosoUniversity.DAL;
using ContosoUniversity.Middleware;
using ContosoUniversity.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoUniversity
{
    /// <summary>
    /// Replaces the IIS-hosted System.Web.HttpApplication (Global.asax). Application start-up
    /// logic and the IIS module/handler behaviour now live in the ASP.NET Core middleware
    /// pipeline, which runs on Kestrel (e.g. Amazon ECS / EKS).
    /// </summary>
    public class MvcApplication
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // Former Application_Start(): instance-specific initialization has been replaced by
            // centralized configuration (AWS Systems Manager Parameter Store) and a DynamoDB-backed
            // distributed lock so cluster-wide initialization happens exactly once.
            DistributedStartupInitializer.LoadSharedConfiguration();

            // Per-process, in-memory registrations (must run on every instance).
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            DbInterception.Add(new SchoolInterceptorLogging());

            try
            {
                // Cluster-wide one-time work: run the EF database initializer once across all instances.
                DistributedStartupInitializer.RunOnce("database-initialize", () =>
                {
                    using (var context = new SchoolContext())
                    {
                        context.Database.Initialize(false);
                    }
                });
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Distributed database initialization failed: " + ex.Message);
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.EnvironmentName == "Development")
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // Content-Security-Policy header (Kestrel equivalent of ContentSecurityPolicyModule)
            app.UseMiddleware<ContentSecurityPolicyMiddleware>();

            // Replaces IIS BlockViewHandler from Views/Web.config
            app.UseMiddleware<BlockViewsMiddleware>();

            // Replaces bundling of ~/Scripts, ~/Content and static resources
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
