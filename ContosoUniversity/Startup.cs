using ContosoUniversity.DAL;
using ContosoUniversity.Middleware;
using ContosoUniversity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.Entity.Infrastructure.Interception;

namespace ContosoUniversity
{
    /// <summary>
    /// ASP.NET Core startup replacing the IIS-hosted System.Web.HttpApplication (Global.asax).
    /// Runs on Kestrel (e.g. Amazon ECS / EKS). Cross-cutting IIS module/handler behaviour is
    /// expressed as middleware in the request pipeline.
    /// </summary>
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddHealthChecks();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Shared configuration from AWS Systems Manager Parameter Store (SSM_PARAMETER_PATH) and
            // one-time initialization guarded by a DynamoDB distributed lock (INIT_LOCK_TABLE), so
            // all scaled instances get consistent setup (replaces Global.asax Application_Start state).
            var initializer = new DistributedInitializer();
            initializer.LoadSharedConfigurationAsync().GetAwaiter().GetResult();
            initializer.RunOnceAsync("contoso-university-db-init", () =>
            {
                using (var db = new SchoolContext())
                {
                    db.Database.Initialize(false);
                }
                return System.Threading.Tasks.Task.FromResult(0);
            }).GetAwaiter().GetResult();

            // Per-process registration (must run on every instance): register EF6 interceptors
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            DbInterception.Add(new SchoolInterceptorLogging());

            // Equivalent of FilterConfig HandleErrorAttribute
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // Content-Security-Policy and related security headers (SOC2 / pen-test compliance)
            app.UseMiddleware<ContentSecurityPolicyMiddleware>();

            // Replaces BlockViewHandler from Views/Web.config
            app.UseMiddleware<BlockViewAccessMiddleware>();

            // Replaces System.Web.Optimization bundling: serve Content/Scripts/fonts directly
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
