using ContosoUniversity.DAL;
using ContosoUniversity.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews(options =>
{
    // Global antiforgery validation for all unsafe HTTP methods.
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddSingleton<SchoolInterceptorLogging>();

var connectionString = builder.Configuration.GetConnectionString("SchoolContext")
    ?? throw new InvalidOperationException("Connection string 'SchoolContext' not found.");

builder.Services.AddDbContext<SchoolContext>((serviceProvider, options) =>
{
    options.UseNpgsql(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 5));
    options.AddInterceptors(serviceProvider.GetRequiredService<SchoolInterceptorLogging>());
});

if (!string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

var app = builder.Build();

// Create the database (if needed) and seed it.
if (builder.Configuration.GetValue("Database:InitializeOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
        await context.Database.EnsureCreatedAsync();
        await DbInitializer.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while creating or seeding the database.");
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
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
