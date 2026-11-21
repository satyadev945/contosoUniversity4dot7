using ContosoUniversity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Logging (console/debug providers are registered by default; configuration is read from appsettings.json).
builder.Logging.AddConsole();

// Application Insights (replaces the classic Application Insights Web SDK / HttpModule).
// Telemetry is only enabled when ApplicationInsights:ConnectionString (or APPLICATIONINSIGHTS_CONNECTION_STRING) is configured.
if (!string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"])
    || !string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// MVC with global antiforgery validation for all unsafe HTTP methods (replaces per-action defaults).
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Bundling and minification (replaces System.Web.Optimization BundleConfig).
builder.Services.AddWebOptimizer(pipeline =>
{
    pipeline.AddCssBundle("/css/bundle.css",
        "lib/bootstrap/css/bootstrap.min.css",
        "css/Site.css");
    pipeline.AddJavaScriptBundle("/js/jquery.bundle.js", "lib/jquery/jquery.min.js");
    pipeline.AddJavaScriptBundle("/js/bootstrap.bundle.js", "lib/bootstrap/js/bootstrap.min.js");
    pipeline.AddJavaScriptBundle("/js/jqueryval.bundle.js",
        "lib/jquery-validation/jquery.validate.min.js",
        "lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js");
});

// Entity Framework Core (replaces EF6 SchoolContext, DbConfiguration and DbInterception).
builder.Services.AddSingleton<SchoolInterceptorLogging>();
builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
        ?? throw new InvalidOperationException("Connection string 'SchoolContext' not found.");

    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure());
    options.AddInterceptors(serviceProvider.GetRequiredService<SchoolInterceptorLogging>());
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Home/Error");
    app.UseHsts();
}
else
{
    // Create/migrate the database and seed sample data in development only.
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

app.UseHttpsRedirection();
app.UseWebOptimizer();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
