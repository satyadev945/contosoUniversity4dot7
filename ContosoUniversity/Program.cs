using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.Entity.Infrastructure.Interception;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddApplicationInsightsTelemetry();

// Connection string is supplied via the ConnectionStrings__SchoolContext environment variable
// (or appsettings.json / mounted secret) so no Windows-only LocalDB is required.
builder.Services.AddScoped(sp =>
    new SchoolContext(builder.Configuration.GetConnectionString("SchoolContext")));

builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

DbInterception.Add(new SchoolInterceptorTransientErrors());
DbInterception.Add(new SchoolInterceptorLogging());

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
