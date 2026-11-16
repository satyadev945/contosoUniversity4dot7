using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace ContosoUniversity
{
    /// <summary>
    /// ASP.NET Core startup replacing the IIS-bound System.Web.HttpApplication (Global.asax).
    /// Hosted by Kestrel, so it can run on Google Kubernetes Engine without IIS.
    /// </summary>
    public class MvcApplication
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // Replaces Global.asax Application_Start. Per-instance work is limited to idempotent,
            // stateless registration. Shared one-time initialization (DB migration/seed) runs in a
            // coordinated Cloud Run init step (APP_RUN_MODE=init) instead of in every scaled instance,
            // and configuration is sourced from GCP Secret Manager.
            CloudInitializer.LoadSecretsIntoEnvironment();

            if (CloudInitializer.IsInitMode())
            {
                CloudInitializer.RunOneTimeInitialization();
                System.Environment.Exit(0);
            }

            CloudInitializer.RegisterInterceptors();
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Replaces HandleErrorAttribute / customErrors
                app.UseExceptionHandler("/Home/Error");
            }

            // Replaces the IIS BlockViewHandler (system.webServer/handlers) from Views/Web.config
            app.UseMiddleware<BlockViewRequestsMiddleware>();

            // Content-Security-Policy header (Kestrel equivalent of ContentSecurityPolicyModule)
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    var headerName = Security.ContentSecurityPolicy.IsReportOnly()
                        ? Security.ContentSecurityPolicy.ReportOnlyHeaderName
                        : Security.ContentSecurityPolicy.HeaderName;
                    context.Response.Headers[headerName] = Security.ContentSecurityPolicy.GetPolicy();
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    return Task.CompletedTask;
                });
                await next();
            });

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

    /// <summary>
    /// ASP.NET Core middleware replacement for the IIS HttpNotFoundHandler registered for the Views folder.
    /// </summary>
    public class BlockViewRequestsMiddleware
    {
        private readonly RequestDelegate _next;

        public BlockViewRequestsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/Views", System.StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }

            return _next(context);
        }
    }
}
