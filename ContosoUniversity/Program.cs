using ContosoUniversity.DAL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC (replaces AreaRegistration / FilterConfig / RouteConfig from Global.asax)
builder.Services.AddControllersWithViews(options =>
{
    // Keep legacy validation behaviour: non-nullable reference types are not implicitly [Required].
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// Telemetry (replaces the Application Insights HTTP module)
builder.Services.AddApplicationInsightsTelemetry();

// Entity Framework Core (replaces EF6 SchoolContext, SchoolConfiguration and DbInterception)
builder.Services.AddSingleton<SchoolInterceptorLogging>();
builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
        ?? throw new InvalidOperationException("Connection string 'SchoolContext' not found.");

    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    options.AddInterceptors(serviceProvider.GetRequiredService<SchoolInterceptorLogging>());
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

// Create / update the database and seed sample data.
if (builder.Configuration.GetValue("Database:InitializeOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
        await context.Database.MigrateAsync();
        await DbInitializer.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseMigrationsEndPoint();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
