using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ContosoUniversity
{
    /// <summary>
    /// ASP.NET Core middleware pipeline replacing IIS modules/handlers previously declared in
    /// Web.config (system.webServer: modules, handlers) and Global.asax (HttpApplication).
    /// </summary>
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddHostedService<ApplicationInitializationService>();   // replaces Global.asax Application_Start (EF interceptors)
            services.AddResponseCompression();   // replaces IIS dynamic/static compression module
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();
            // Content-Security-Policy (SOC2 / Azure Policy); overridable via ContentSecurityPolicy env var / app setting
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["Content-Security-Policy"] = ContosoUniversity.Security.ContentSecurityPolicyModule.GetPolicy();
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    return System.Threading.Tasks.Task.CompletedTask;
                });
                await next();
            });
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");   // replaces HandleErrorAttribute / customErrors
            }

            app.UseResponseCompression();
            // Views are never exposed as static content (replaces the BlockViewHandler
            // HttpNotFoundHandler in Views/Web.config); only wwwroot is served.
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
