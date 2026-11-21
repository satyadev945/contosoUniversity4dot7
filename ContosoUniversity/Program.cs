using ContosoUniversity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC with global antiforgery validation for all unsafe HTTP verbs.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Application Insights (connection string comes from ApplicationInsights:ConnectionString or environment variables).
builder.Services.AddApplicationInsightsTelemetry();

// Entity Framework Core with SQL command logging interceptor and transient-fault resilience.
builder.Services.AddSingleton<SchoolInterceptorLogging>();
builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
        ?? throw new InvalidOperationException("Connection string 'SchoolContext' not found.");
    var maxRetryCount = builder.Configuration.GetValue("Database:MaxRetryCount", 5);

    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount));
    options.AddInterceptors(serviceProvider.GetRequiredService<SchoolInterceptorLogging>());
});

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsAndSeedOnStartup"))
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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
