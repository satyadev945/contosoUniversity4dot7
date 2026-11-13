using System;
using System.Data.Entity.Infrastructure.Interception;
using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Connection string is supplied through the environment (container friendly):
//   ConnectionStrings__SchoolContext  or  DB_CONNECTION_STRING
var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
                       ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

builder.Services.AddControllersWithViews();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddHealthChecks();
builder.Services.AddScoped(_ =>
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Database connection string not configured. Set 'ConnectionStrings__SchoolContext' or 'DB_CONNECTION_STRING'.");
    }
    return new SchoolContext(connectionString);
});

var app = builder.Build();

DbInterception.Add(new SchoolInterceptorTransientErrors());
DbInterception.Add(new SchoolInterceptorLogging());

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

app.Run();
