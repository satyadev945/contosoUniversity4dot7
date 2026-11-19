using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC with global anti-forgery validation for all unsafe HTTP methods (POST, PUT, DELETE).
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Application Insights for ASP.NET Core (connection string comes from configuration).
builder.Services.AddApplicationInsightsTelemetry();

// Entity Framework Core with connection resiliency (replaces SqlAzureExecutionStrategy).
var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
    ?? throw new InvalidOperationException("Connection string 'SchoolContext' not found.");
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

var app = builder.Build();

// Optional database creation / seeding (replaces Database initializers and migration Seed).
var applyMigrations = app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");
var seedOnStartup = app.Configuration.GetValue<bool>("Database:SeedOnStartup");
if (applyMigrations || seedOnStartup)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
        if (applyMigrations)
        {
            await context.Database.MigrateAsync();
        }
        if (seedOnStartup)
        {
            await DbInitializer.InitializeAsync(context);
        }
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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
