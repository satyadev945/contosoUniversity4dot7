using ContosoUniversity.DAL;
using ContosoUniversity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Data.Entity.Infrastructure.Interception;

namespace ContosoUniversity
{
    /// <summary>
    /// ASP.NET Core startup replacing the IIS-bound System.Web.HttpApplication (Global.asax).
    /// The request pipeline is composed of middleware hosted on Kestrel.
    /// </summary>
    public class Startup
    {
        public Startup()
        {
            // Centralized configuration: load shared settings from AWS Systems Manager Parameter Store
            // (SSM_PARAMETER_PATH) so every scaled instance starts with identical configuration.
            ParameterStoreConfigurationProvider.ApplyToEnvironment();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Replaces FilterConfig/GlobalFilters (HandleErrorAttribute is replaced by exception-handler middleware below)
            services.AddControllersWithViews();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Replaces HandleErrorAttribute / customErrors
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // Content-Security-Policy headers (Kestrel equivalent of ContentSecurityPolicyModule)
            app.Use(async (context, next) =>
            {
                context.Response.Headers["Content-Security-Policy"] = ContosoUniversity.Security.ContentSecurityPolicyModule.GetPolicy();
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                await next();
            });

            // Replaces IIS static file handler and BundleConfig (static assets served by middleware)
            app.UseStaticFiles();

            // Replaces RouteConfig.RegisterRoutes(RouteTable.Routes)
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            // Replaces Application_Start database interceptor registration.
            // Interceptors are process-local and stateless, so each instance registers them itself.
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            DbInterception.Add(new SchoolInterceptorLogging());

            // Cluster-wide one-time initialization (database creation/seed) is coordinated through a
            // DynamoDB distributed lock so that only one scaled instance performs it (INIT_LOCK_TABLE).
            try
            {
                DistributedInitializer.RunOnce("school-database", () =>
                {
                    using (var context = new SchoolContext())
                    {
                        context.Database.Initialize(false);
                    }
                });
            }
            catch (Exception ex)
            {
                // Preserve previous lazy-initialization behaviour if coordination fails at startup.
                Console.Error.WriteLine("{\"level\":\"Warning\",\"message\":\"Distributed initialization failed\",\"error\":\"" + ex.Message.Replace("\"", "'") + "\"}");
            }
        }
    }
}
