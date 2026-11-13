using ContosoUniversity.DAL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Tests
{
    /// <summary>Context whose SaveChangesAsync always fails, used to exercise error handling paths.</summary>
    public class ThrowingSchoolContext : SchoolContext
    {
        public Exception ToThrow { get; set; } = new DbUpdateException("boom");

        public ThrowingSchoolContext(DbContextOptions<SchoolContext> options) : base(options) { }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            throw ToThrow;
        }
    }

    public static class TestHelpers
    {
        public static DbContextOptions<SchoolContext> CreateOptions(string name = null)
        {
            return new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
        }

        public static SchoolContext CreateContext(string name = null)
        {
            return new SchoolContext(CreateOptions(name));
        }

        public static ThrowingSchoolContext CreateThrowingContext(string name = null)
        {
            return new ThrowingSchoolContext(CreateOptions(name));
        }

        public static T WithHttpContext<T>(this T controller) where T : Controller
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddControllersWithViews();
            var provider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = provider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());
            controller.ControllerContext = new ControllerContext(actionContext);
            controller.ControllerContext.ValueProviderFactories.Add(new FormValueProviderFactory());
            return controller;
        }

        public static void SetForm(this Controller controller, Dictionary<string, string> values)
        {
            var request = controller.HttpContext.Request;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Form = new FormCollection(values.ToDictionary(
                kv => kv.Key, kv => new Microsoft.Extensions.Primitives.StringValues(kv.Value)));
        }
    }
}
