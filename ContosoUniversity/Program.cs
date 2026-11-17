using ContosoUniversity.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews();

// Telemetry (replaces the Application Insights HTTP module); only enabled when a connection string is configured.
if (!string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// EF Core (replaces EF6 SchoolContext, SchoolConfiguration and DbInterception registration)
builder.Services.AddSingleton<SchoolInterceptorLogging>();
builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure(maxRetryCount: 5));
    options.AddInterceptors(serviceProvider.GetRequiredService<SchoolInterceptorLogging>());
});

var app = builder.Build();

// Apply migrations and seed data
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
    }
}

// HTTP pipeline (replaces HandleErrorAttribute / FilterConfig)
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

// Replaces RouteConfig
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
