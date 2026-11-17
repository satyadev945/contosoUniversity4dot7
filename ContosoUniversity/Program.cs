using ContosoUniversity.Data;
using ContosoUniversity.Data.Interceptors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// MVC with global anti-forgery validation for all unsafe HTTP methods (replaces per-action attributes + FilterConfig).
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Application Insights (replaces the System.Web HTTP module). Only enabled when a connection string is configured.
if (!string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// EF Core interceptors (replace DbInterception.Add in Global.asax).
builder.Services.AddSingleton<SchoolInterceptorLogging>();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<SchoolInterceptorTransientErrors>();
}

// Entity Framework Core with connection resiliency (replaces SchoolConfiguration / SqlAzureExecutionStrategy).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    options.AddInterceptors(serviceProvider.GetRequiredService<SchoolInterceptorLogging>());
    if (builder.Environment.IsDevelopment())
    {
        options.AddInterceptors(serviceProvider.GetRequiredService<SchoolInterceptorTransientErrors>());
    }
});

var app = builder.Build();

// Apply migrations and seed data at startup (replaces SchoolInitializer / Migrations.Configuration.Seed).
if (app.Configuration.GetValue("Database:InitializeOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
        await context.Database.MigrateAsync();
        await DbSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

app.UseHttpsRedirection();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = new[] { new System.Globalization.CultureInfo("en-US") },
    SupportedUICultures = new[] { new System.Globalization.CultureInfo("en-US") }
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
