using System;
using System.Diagnostics;
using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Route Trace output (used by ContosoUniversity.Logging.Logger) to stdout so it is visible in container logs.
Trace.Listeners.Add(new ConsoleTraceListener());

var builder = WebApplication.CreateBuilder(args);

// Application settings are no longer read from Views/Web.config <appSettings> (webpages:Enabled is an
// ASP.NET Web Pages setting with no ASP.NET Core equivalent). Configuration is layered as appsettings.json ->
// environment variables (e.g. injected from AWS SSM Parameter Store via the ECS task definition "secrets" array).
builder.Configuration.AddEnvironmentVariables();

// Connection string comes from configuration (ConnectionStrings__SchoolContext env var)
// or is assembled from DB_* environment variables.
string connectionString = builder.Configuration.GetConnectionString("SchoolContext")
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? string.Format(
        "Server={0},{1};Database={2};User Id={3};Password={4};TrustServerCertificate={5};",
        Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost",
        Environment.GetEnvironmentVariable("DB_PORT") ?? "1433",
        Environment.GetEnvironmentVariable("DB_NAME") ?? "ContosoUniversity",
        Environment.GetEnvironmentVariable("DB_USER") ?? "sa",
        Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty,
        Environment.GetEnvironmentVariable("DB_TRUST_SERVER_CERTIFICATE") ?? "true");

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseLazyLoadingProxies()
           .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
           .AddInterceptors(new SchoolInterceptorLogging()));
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapHealthChecks("/health");
app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (string.Equals(Environment.GetEnvironmentVariable("DB_INITIALIZE"), "true", StringComparison.OrdinalIgnoreCase))
{
    using (var scope = app.Services.CreateScope())
    {
        SchoolInitializer.Initialize(scope.ServiceProvider.GetRequiredService<SchoolContext>());
    }
}

app.Run();
