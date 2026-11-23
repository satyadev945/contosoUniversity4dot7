using ContosoUniversity.DAL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string is supplied through configuration / environment variables, e.g.
//   ConnectionStrings__SchoolContext="Server=${DB_HOST};Database=${DB_NAME};User Id=${DB_USER};Password=${DB_PASSWORD};TrustServerCertificate=True"
var connectionString = builder.Configuration.GetConnectionString("SchoolContext");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var host = Environment.GetEnvironmentVariable("DB_HOST");
    var name = Environment.GetEnvironmentVariable("DB_NAME") ?? "ContosoUniversity";
    var user = Environment.GetEnvironmentVariable("DB_USER");
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
    if (string.IsNullOrWhiteSpace(host))
    {
        throw new InvalidOperationException(
            "Database configuration missing. Set ConnectionStrings__SchoolContext or DB_HOST/DB_NAME/DB_USER/DB_PASSWORD.");
    }
    connectionString = $"Server={host};Database={name};User Id={user};Password={password};TrustServerCertificate=True";
}

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Create/seed the database on startup (replaces EF6 initializer/migrations configuration)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
        context.Database.EnsureCreated();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while creating/seeding the database.");
    }
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
