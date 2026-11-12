using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Connection string is supplied via configuration / environment variable:
//   ConnectionStrings__SchoolContext
var connectionString = builder.Configuration.GetConnectionString("SchoolContext");

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<SchoolInterceptorLogging>();
builder.Services.AddDbContext<SchoolContext>((sp, options) =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
           .AddInterceptors(sp.GetRequiredService<SchoolInterceptorLogging>()));

// Health checks (used by container liveness/readiness probes)
builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    SchoolInitializer.Initialize(scope.ServiceProvider.GetRequiredService<SchoolContext>());
}

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

app.Run();
