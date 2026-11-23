using System.Diagnostics;
using ContosoUniversity.DAL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Surface System.Diagnostics.Trace output (used by ContosoUniversity.Logging.Logger) on stdout for container logs.
Trace.Listeners.Add(new ConsoleTraceListener());

builder.Services.AddControllersWithViews();

// Connection string comes from configuration (ConnectionStrings__SchoolContext env var)
// or is assembled from DB_HOST / DB_PORT / DB_NAME / DB_USER / DB_PASSWORD environment variables.
string connectionString = builder.Configuration.GetConnectionString("SchoolContext");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    var name = Environment.GetEnvironmentVariable("DB_NAME") ?? "ContosoUniversity";
    var user = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty;
    connectionString = $"Host={host};Port={port};Database={name};Username={user};Password={password}";
}

builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseNpgsql(connectionString, sql => sql.EnableRetryOnFailure())
           .AddInterceptors(new SchoolInterceptorLogging()));

builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    SchoolInitializer.Initialize(context);
}

app.Run();
