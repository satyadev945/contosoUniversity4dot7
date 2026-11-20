using ContosoUniversity.Initialization;
using ContosoUniversity.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ContosoUniversity
{
    /// <summary>
    /// Replaces Global.asax / MvcApplication (System.Web.HttpApplication, an IIS-hosted module).
    /// Application_Start logic and the IIS HttpModules/Handlers are expressed as ASP.NET Core middleware.
    /// </summary>
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Replaces FilterConfig / BundleConfig / AreaRegistration registration.
            services.AddControllersWithViews();

            // HtmlSanitizer (Ganss.Xss) with its allow-list stored in AWS Secrets Manager
            // (secret id from HTML_SANITIZER_SECRET_ID). Loaded once and shared (stateless, thread-safe).
            var sanitizerOptions = HtmlSanitizerOptionsProvider.Load();
            services.AddSingleton(sanitizerOptions);
            services.AddSingleton<IHtmlSanitizerService, HtmlSanitizerService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Former Application_Start initialisation: configuration from AWS Parameter Store and
            // cluster-wide coordination through a DynamoDB lock (see DistributedStartupInitializer).
            DistributedStartupInitializer.Initialize();

            // Replaces HandleErrorAttribute global filter.
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // Replaces IIS static file handler; only wwwroot is exposed, so Views are never served
            // (equivalent of the former BlockViewHandler in Views/Web.config).
            app.UseStaticFiles();

            app.UseRouting();

            // Replaces RouteConfig.RegisterRoutes.
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
