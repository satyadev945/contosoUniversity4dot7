using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Logging (console/debug providers are configured by default; levels come from appsettings.json).

// Application Insights (replaces the ApplicationInsights HTTP module / ApplicationInsights.config).
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// MVC with a global anti-forgery filter for all unsafe HTTP methods
// (replaces FilterConfig / HandleErrorAttribute).
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Entity Framework Core (replaces EF6 + SchoolConfiguration / SqlAzureExecutionStrategy).
var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
    ?? throw new InvalidOperationException("Connection string 'SchoolContext' not found.");

builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

var app = builder.Build();

// Create/migrate and seed the database (replaces SchoolInitializer / Migrations.Configuration.Seed).
if (app.Configuration.GetValue("Database:InitializeOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
        await DbInitializer.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Exposed for integration tests (WebApplicationFactory<Program>).
public partial class Program { }
