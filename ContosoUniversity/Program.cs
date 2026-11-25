using System;
using System.Diagnostics;
using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Send System.Diagnostics.Trace output (used by ContosoUniversity.Logging.Logger) to stdout for container logging
Trace.Listeners.Add(new ConsoleTraceListener());

var builder = WebApplication.CreateBuilder(args);

// Connection string is supplied via environment variable ConnectionStrings__SchoolContext
var connectionString = builder.Configuration.GetConnectionString("SchoolContext");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'SchoolContext' is not configured. Set the ConnectionStrings__SchoolContext environment variable.");
}

builder.Services.AddControllersWithViews();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSingleton<SchoolInterceptorLogging>();
builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
           .AddInterceptors(serviceProvider.GetRequiredService<SchoolInterceptorLogging>()));
builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    SchoolInitializer.Initialize(scope.ServiceProvider.GetRequiredService<SchoolContext>());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
