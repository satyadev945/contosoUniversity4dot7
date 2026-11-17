using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// The connection string is read from configuration and can be overridden with the
// ConnectionStrings__SchoolContext environment variable (e.g. injected from Secret Manager).
var connectionString = builder.Configuration.GetConnectionString("SchoolContext");

builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseLazyLoadingProxies()
           .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

builder.Services.AddControllersWithViews();

builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    SchoolInitializer.Initialize(context);
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
