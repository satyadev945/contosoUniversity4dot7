using ContosoUniversity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC with global antiforgery validation on all unsafe HTTP verbs (replaces per-action [ValidateAntiForgeryToken] reliance).
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    // Keep legacy validation behavior: reference types are optional unless annotated with [Required].
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// Entity Framework Core (replaces EF6 SchoolContext + SchoolConfiguration + DbInterception).
builder.Services.AddTransient<SchoolCommandLoggingInterceptor>();
builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
        ?? throw new InvalidOperationException("Connection string 'SchoolContext' was not found.");

    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    options.AddInterceptors(serviceProvider.GetRequiredService<SchoolCommandLoggingInterceptor>());
});

// Telemetry (replaces ApplicationInsightsHttpModule). Enabled only when a connection string is configured.
if (!string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Apply migrations and optional seed data at startup.
var databaseSection = app.Configuration.GetSection("Database");
if (databaseSection.GetValue("MigrateOnStartup", false))
{
    await DatabaseInitializer.InitializeAsync(
        app.Services,
        baselineLegacyDatabase: databaseSection.GetValue("BaselineLegacyDatabase", false),
        seed: databaseSection.GetValue("Seed", false));
}

app.Run();

public partial class Program
{
}
