using ContosoUniversity.DAL;
using ContosoUniversity.Infrastructure;
using ContosoUniversity.Logging;
using ContosoUniversity.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Data.Entity.Infrastructure.Interception;

namespace ContosoUniversity
{
    /// <summary>
    /// Replaces Global.asax / MvcApplication (System.Web.HttpApplication) and the IIS
    /// handler configuration in Views/Web.config with an ASP.NET Core middleware pipeline
    /// that runs on Kestrel (e.g. Amazon ECS / EKS).
    /// </summary>
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Trust X-Forwarded-* headers from the AWS load balancer (ALB) in front of Elastic Beanstalk.
            services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                    | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            // Replaces AreaRegistration / FilterConfig / RouteConfig registration.
            services.AddControllersWithViews();
            services.AddHealthChecks();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Process-local registration: must run on every instance (replaces Application_Start).
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            DbInterception.Add(new SchoolInterceptorLogging());

            // Cluster-wide initialization: shared settings come from AWS Systems Manager Parameter Store and
            // one-time setup is guarded by a DynamoDB lock so only one scaled instance performs it.
            RunDistributedInitialization();

            // Replaces HandleErrorAttribute global filter / customErrors.
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // Honour X-Forwarded-* headers set by the AWS load balancer.
            app.UseForwardedHeaders();

            // Content-Security-Policy and related security headers (SOC2 / pen-test compliance).
            app.UseMiddleware<ContentSecurityPolicyMiddleware>();

            // Replaces the IIS "BlockViewHandler" (HttpNotFoundHandler) in Views/Web.config:
            // view files are never served directly as static content.
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path;
                if (path.StartsWithSegments("/Views", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                await next();
            });

            // Static content (replaces BundleConfig / IIS static file handler).
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                // Replaces RouteConfig.RegisterRoutes.
                endpoints.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private static void RunDistributedInitialization()
        {
            var logger = new Logger();
            try
            {
                var initializer = new DistributedInitializer(logger);
                var config = initializer.LoadSharedConfigurationAsync().GetAwaiter().GetResult();
                logger.Information("Loaded {0} shared parameter(s) from AWS Parameter Store.", config.Count);

                // One-time, cluster-wide initialization hook (executed by a single instance per version).
                initializer.EnsureInitializedAsync(
                    Environment.GetEnvironmentVariable("INIT_VERSION") ?? "1",
                    () => System.Threading.Tasks.Task.CompletedTask).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Distributed initialization failed; continuing with local configuration.");
            }
        }
    }
}
