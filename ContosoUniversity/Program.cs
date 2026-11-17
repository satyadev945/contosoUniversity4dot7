using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Logging (providers are configured through appsettings.json)
builder.Logging.AddConsole();

// MVC with global antiforgery validation (replaces FilterConfig / HandleErrorAttribute)
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Application Insights (replaces the Application Insights Web SDK HTTP module)
builder.Services.AddApplicationInsightsTelemetry();

// Entity Framework Core (replaces EF6 DbConfiguration / DbInterception)
builder.Services.AddScoped<SchoolCommandLoggingInterceptor>();
builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
        ?? throw new InvalidOperationException("Connection string 'SchoolContext' not found.");

    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    options.AddInterceptors(serviceProvider.GetRequiredService<SchoolCommandLoggingInterceptor>());
});

var app = builder.Build();

// Seed the database when the "Database:SeedOnStartup" setting is enabled (Development by default)
if (builder.Configuration.GetValue<bool>("Database:SeedOnStartup"))
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

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
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

// Exposed for integration tests (WebApplicationFactory<Program>)
public partial class Program { }
