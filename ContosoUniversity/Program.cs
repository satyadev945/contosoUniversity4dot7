using ContosoUniversity.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC with views. Non-nullable reference types are not implicitly [Required] (matches the legacy behavior).
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// Application Insights (replaces the .NET Framework web SDK / HTTP module).
// Telemetry is enabled when ApplicationInsights:ConnectionString is configured.
if (!string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// Entity Framework Core (replaces EF6 DbContext, SqlAzureExecutionStrategy and DbInterception).
builder.Services.AddSingleton<SchoolCommandInterceptor>();
builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
        ?? throw new InvalidOperationException("Connection string 'SchoolContext' not found.");

    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    options.AddInterceptors(serviceProvider.GetRequiredService<SchoolCommandInterceptor>());
});

var app = builder.Build();

// Apply migrations and seed data.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
        await DbInitializer.InitializeAsync(context, logger);
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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
