using System.Reflection;
using ContosoUniversity.Controllers;
using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ContosoUniversity.ProgramTests
{
    public class ProgramTests
    {
        private static readonly Assembly AppAssembly = typeof(HomeController).Assembly;

        [Fact]
        public void ProgramEntryPoint_Exists()
        {
            Assert.NotNull(AppAssembly.EntryPoint);
            Assert.NotNull(AppAssembly.GetType("Program"));
        }

        [Fact]
        public void Controllers_AreDiscoverableMvcControllers()
        {
            var controllers = AppAssembly.GetTypes().Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract).Select(t => t.Name).ToList();
            Assert.Contains("HomeController", controllers);
            Assert.Contains("StudentsController", controllers);
            Assert.Contains("CourseController", controllers);
            Assert.Contains("InstructorController", controllers);
        }

        [Fact]
        public void ServiceRegistration_ResolvesControllerWithContextAndInterceptor()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<SchoolInterceptorLogging>();
            services.AddDbContext<SchoolContext>((sp, options) =>
            {
                options.UseInMemoryDatabase("program-di");
                options.AddInterceptors(sp.GetRequiredService<SchoolInterceptorLogging>());
            });
            services.AddTransient<HomeController>();
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<HomeController>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<SchoolContext>());
        }

        [Fact]
        public void AntiforgeryFilter_CanBeAddedToMvcOptions()
        {
            var options = new MvcOptions();
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            Assert.Single(options.Filters);
        }
    }
}
