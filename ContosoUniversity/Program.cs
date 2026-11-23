using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.Entity.Infrastructure.Interception;
using ContosoUniversity.DAL;

var builder = WebApplication.CreateBuilder(args);

// Configuration is supplied via environment variables (Kubernetes ConfigMaps/Secrets) instead of
// web.config appSettings. Environment variables override any appsettings*.json values at runtime.
// The legacy Views/Web.config 'webpages:Enabled=false' setting is not needed: ASP.NET Core never serves .cshtml directly.
builder.Configuration.AddEnvironmentVariables();

// ASP.NET Core MVC (Razor views) - runs on Linux containers
builder.Services.AddControllersWithViews();

// Health check endpoint for container orchestration (liveness/readiness probes)
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

DbInterception.Add(new SchoolInterceptorTransientErrors());
DbInterception.Add(new SchoolInterceptorLogging());

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
