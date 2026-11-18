using System.Data.Entity.Infrastructure.Interception;
using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddHealthChecks();

// Entity Framework 6 command interceptors (formerly registered in Global.asax Application_Start)
DbInterception.Add(new SchoolInterceptorTransientErrors());
DbInterception.Add(new SchoolInterceptorLogging());

var app = builder.Build();

// Static files and routing pipeline
app.UseStaticFiles();
app.UseRouting();

app.MapHealthChecks("/health");
app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
