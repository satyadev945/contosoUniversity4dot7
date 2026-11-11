using ContosoUniversity.DAL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string is supplied via the ConnectionStrings__SchoolContext environment variable
// (or SCHOOL_DB_CONNECTION), keeping the container image free of environment-specific settings.
var connectionString = Environment.GetEnvironmentVariable("SCHOOL_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("SchoolContext");

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

// Liveness endpoint does not depend on the database; readiness (/health/ready) checks the DB.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SchoolContext>("database", tags: new[] { "ready" });

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(connectionString))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    try
    {
        SchoolInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database initialization failed; /health/ready will report the database as unhealthy.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
