using System.Data.Entity.Infrastructure.Interception;
using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();

ConnectionStringProvider.Configure(builder.Configuration.GetConnectionString("SchoolContext"));

DbInterception.Add(new SchoolInterceptorTransientErrors());
DbInterception.Add(new SchoolInterceptorLogging());

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

app.Run();
