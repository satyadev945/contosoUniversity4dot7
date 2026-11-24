using System.Data.Entity.Infrastructure.Interception;
using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<SchoolContext>();
builder.Services.AddHealthChecks();

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
    name: "Default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
