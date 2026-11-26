using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Logging (console/debug providers are configured by default; configuration comes from appsettings.json)
builder.Logging.AddConsole();

// MVC with global anti-forgery validation for all unsafe HTTP verbs
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Application Insights (connection string comes from configuration: ApplicationInsights:ConnectionString)
builder.Services.AddApplicationInsightsTelemetry();

// Entity Framework Core
builder.Services.AddSingleton<SchoolCommandInterceptor>();
builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
        ?? throw new InvalidOperationException("Connection string 'SchoolContext' was not found.");

    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure());
    options.AddInterceptors(serviceProvider.GetRequiredService<SchoolCommandInterceptor>());
});

var app = builder.Build();

// Optionally apply migrations and seed the database at startup
if (app.Configuration.GetValue<bool>("Database:InitializeOnStartup"))
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
        throw;
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

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
