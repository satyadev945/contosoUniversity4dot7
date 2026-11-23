using ContosoUniversity.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ContosoUniversity.AspNetCore
{
    /// <summary>
    /// Kestrel-hosted pipeline replacing IIS modules/handlers and Global.asax:
    ///  - ApplicationInsightsWebTracking module -> Application Insights SDK for ASP.NET Core
    ///  - BlockViewHandler                      -> BlockViewsMiddleware
    ///  - IIS dynamic compression               -> Response Compression middleware
    ///  - IIS static file handler               -> Static Files middleware
    ///  - Global.asax Application_Start         -> StartupInitializationHostedService (IHostedService)
    /// </summary>
    public class Startup
    {
        private const string DefaultContentSecurityPolicy =
            "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; " +
            "font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; " +
            "form-action 'self'; frame-ancestors 'none'";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry();
            services.AddResponseCompression();
            services.AddHealthChecks();
            services.AddHostedService<StartupInitializationHostedService>();
            services.AddSingleton<IHtmlSanitizerService>(sp => HtmlSanitization.Current);
            services.AddControllersWithViews();
            services.Configure<ForwardedHeadersOptions>(o =>
            {
                o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                o.KnownNetworks.Clear();
                o.KnownProxies.Clear();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // Content-Security-Policy (ASP.NET Core equivalent of ContentSecurityPolicyModule / web.config customHeaders)
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    var policy = System.Environment.GetEnvironmentVariable("ContentSecurityPolicy");
                    context.Response.Headers["Content-Security-Policy"] = string.IsNullOrWhiteSpace(policy)
                        ? DefaultContentSecurityPolicy
                        : policy;
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    return System.Threading.Tasks.Task.CompletedTask;
                });
                await next();
            });

            app.UseResponseCompression();
            app.UseMiddleware<BlockViewsMiddleware>();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/healthz");
                endpoints.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
