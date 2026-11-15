using System;
using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Connection string is supplied through the environment (ConnectionStrings__SchoolContext)
// or composed from DB_HOST / DB_NAME / DB_USER / DB_PASSWORD.
var connectionString = builder.Configuration.GetConnectionString("SchoolContext");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    var name = Environment.GetEnvironmentVariable("DB_NAME") ?? "ContosoUniversity";
    var user = Environment.GetEnvironmentVariable("DB_USER") ?? "sa";
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty;
    connectionString = $"Server={host};Database={name};User Id={user};Password={password};TrustServerCertificate=True;MultipleActiveResultSets=True";
}

builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddControllersWithViews();
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
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    DbInitializer.Initialize(context);
}

app.Run();
