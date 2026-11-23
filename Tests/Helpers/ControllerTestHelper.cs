using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace ContosoUniversity.Tests.Helpers
{
    public static class ControllerTestHelper
    {
        public static void Setup(Controller controller, Dictionary<string, string> form = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddControllersWithViews();
            var provider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = provider };
            httpContext.TraceIdentifier = "test-trace-id";
            if (form != null)
            {
                httpContext.Request.ContentType = "application/x-www-form-urlencoded";
                var dict = new Dictionary<string, StringValues>();
                foreach (var kv in form)
                {
                    dict[kv.Key] = kv.Value;
                }
                httpContext.Request.Form = new FormCollection(dict);
            }

            var actionContext = new ControllerContext(new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor()))
            {
                ValueProviderFactories = new List<IValueProviderFactory> { new FormValueProviderFactory() }
            };
            controller.ControllerContext = actionContext;
        }

        public static T ModelOf<T>(IActionResult result)
        {
            var view = Assert.IsType<ViewResult>(result);
            return Assert.IsAssignableFrom<T>(view.Model);
        }
    }
}
