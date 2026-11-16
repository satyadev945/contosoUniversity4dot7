using ContosoUniversity.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC with views (replaces AreaRegistration, FilterConfig and RouteConfig from Global.asax).
builder.Services.AddControllersWithViews();

// Entity Framework Core (replaces EF6 DbConfiguration, SqlAzureExecutionStrategy and DbInterception).
var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
    ?? throw new InvalidOperationException("Connection string 'SchoolContext' not found.");

builder.Services.AddSingleton<SchoolCommandInterceptor>();
builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
    options
        .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
        .AddInterceptors(serviceProvider.GetRequiredService<SchoolCommandInterceptor>()));

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

// Application Insights (replaces the System.Web ApplicationInsightsHttpModule).
if (!string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

var app = builder.Build();

// Optional database migration and seeding at startup (replaces the EF6 database initializer).
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();

    try
    {
        if (configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            await context.Database.MigrateAsync();
        }

        if (configuration.GetValue<bool>("Database:SeedOnStartup"))
        {
            await DbInitializer.SeedAsync(context);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

// HTTP request pipeline (replaces HandleErrorAttribute and customErrors).
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
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
