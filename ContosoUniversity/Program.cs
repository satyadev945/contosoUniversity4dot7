using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC with a global anti-forgery validation filter (replaces FilterConfig / HandleErrorAttribute).
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Application Insights (replaces the System.Web HTTP module) - enabled only when configured.
if (!string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// Entity Framework Core (replaces EF6 SchoolContext + SchoolConfiguration + DbInterception).
builder.Services.AddSingleton<SchoolInterceptorLogging>();
builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());

    if (builder.Configuration.GetValue("SchoolDatabase:LogSqlCommands", true))
    {
        options.AddInterceptors(serviceProvider.GetRequiredService<SchoolInterceptorLogging>());
    }
});

var app = builder.Build();

// Apply migrations and seed data at startup.
if (builder.Configuration.GetValue("SchoolDatabase:SeedOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
        await context.Database.MigrateAsync();
        await SchoolInitializer.SeedAsync(context);
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
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

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
