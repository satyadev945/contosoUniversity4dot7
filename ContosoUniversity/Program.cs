using System.Globalization;
using ContosoUniversity.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC with a global antiforgery validation filter (replaces per-action checks and the HandleError filter).
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Command logging interceptor (replaces the EF6 DbInterception registrations).
builder.Services.AddSingleton<SchoolCommandLoggingInterceptor>();

// Entity Framework Core with connection resiliency (replaces SqlAzureExecutionStrategy).
builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
        ?? throw new InvalidOperationException("Connection string 'SchoolContext' not found.");

    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 5))
           .AddInterceptors(serviceProvider.GetRequiredService<SchoolCommandLoggingInterceptor>());
});

// Application Insights (connection string comes from configuration or environment variables).
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// Apply migrations and seed data in Development, or when explicitly enabled in configuration.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:InitializeOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
        DbInitializer.Initialize(context);
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

// Fixed culture so that dates and numbers are parsed consistently.
var supportedCulture = new CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(supportedCulture),
    SupportedCultures = new[] { supportedCulture },
    SupportedUICultures = new[] { supportedCulture }
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
