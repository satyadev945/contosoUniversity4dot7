using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ContosoUniversity.DAL;
using ContosoUniversity.Logging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string is supplied via environment variable ConnectionStrings__SchoolContext
// (or the SCHOOL_DB_CONNECTION shortcut) so that no environment specific values are baked into the image.
var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
    ?? Environment.GetEnvironmentVariable("SCHOOL_DB_CONNECTION")
    ?? throw new InvalidOperationException(
        "Database connection string not configured. Set ConnectionStrings__SchoolContext.");

builder.Services.AddSingleton<ILogger, Logger>();
builder.Services.AddSingleton<SchoolInterceptorLogging>();
builder.Services.AddDbContext<SchoolContext>((sp, options) =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
           .AddInterceptors(sp.GetRequiredService<SchoolInterceptorLogging>()));

builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Create and seed the database on first start (opt out with Database__AutoInitialize=false).
if (builder.Configuration.GetValue("Database:AutoInitialize", true))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    SchoolInitializer.Initialize(context);
}

app.Run();
