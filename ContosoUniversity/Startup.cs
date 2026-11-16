using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ContosoUniversity.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System;
using System.Data.Entity.Infrastructure.Interception;
using Amazon.DynamoDBv2;

namespace ContosoUniversity
{
    /// <summary>
    /// ASP.NET Core startup replacing the IIS-hosted System.Web.HttpApplication (Global.asax).
    /// Hosted by Kestrel; request pipeline is composed of middleware instead of IIS modules/handlers.
    /// </summary>
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            // Centralized configuration from AWS Systems Manager Parameter Store (replaces per-instance
            // Global.asax/Web.config initialization). Path prefix from PARAMETER_STORE_PATH (default /contoso-university).
            // Set PARAMETER_STORE_ENABLED=false to skip (e.g., local development).
            var builder = new ConfigurationBuilder().AddConfiguration(configuration);
            var enabled = configuration["PARAMETER_STORE_ENABLED"];
            if (!string.Equals(enabled, "false", StringComparison.OrdinalIgnoreCase))
            {
                var path = configuration["PARAMETER_STORE_PATH"] ?? "/contoso-university";
                builder.AddSystemsManager(path, optional: true);
            }
            _configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddHealthChecks();

            // AWS ElastiCache (Redis) distributed cache for non-blocking data retrieval in view-rendering actions.
            // Endpoint comes from configuration/environment (ELASTICACHE_ENDPOINT), never hardcoded.
            var redisEndpoint = _configuration["ELASTICACHE_ENDPOINT"] ?? _configuration["ElastiCache:Endpoint"];
            if (!string.IsNullOrWhiteSpace(redisEndpoint))
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisEndpoint;
                    options.InstanceName = "contoso:";
                });
            }
            else
            {
                // Local/dev fallback when no ElastiCache endpoint is configured
                services.AddDistributedMemoryCache();
            }
            services.AddScoped<IStudentCache, StudentCache>();

            // Distributed, run-once-per-cluster initialization via DynamoDB lock (replaces Application_Start work)
            services.AddSingleton<IAmazonDynamoDB>(sp => new AmazonDynamoDBClient());
            services.AddSingleton<IDistributedInitializer, DynamoDbDistributedInitializer>();
            services.AddHostedService<DistributedStartupInitializer>();

            // EF6 interceptors (per-process wiring; must run on every instance)
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            DbInterception.Add(new SchoolInterceptorLogging());
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Replaces <customErrors>/HandleErrorAttribute and IIS error handling
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // Replaces IIS static file handler; static content is served by middleware
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                // Replaces RouteConfig.RegisterRoutes
                endpoints.MapControllerRoute(
                    name: "Default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
