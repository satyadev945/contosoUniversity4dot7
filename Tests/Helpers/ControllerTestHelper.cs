using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoUniversity.Tests.Helpers
{
    public static class ControllerTestHelper
    {
        private static readonly IServiceProvider Services = BuildServices();

        private static IServiceProvider BuildServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddControllersWithViews();
            return services.BuildServiceProvider();
        }

        public static T WithContext<T>(this T controller, Dictionary<string, string>? form = null) where T : Controller
        {
            var http = new DefaultHttpContext { RequestServices = Services };
            http.Request.Method = "POST";
            if (form != null)
            {
                http.Request.ContentType = "application/x-www-form-urlencoded";
                http.Request.Form = new FormCollection(
                    form.ToDictionary(k => k.Key, v => new Microsoft.Extensions.Primitives.StringValues(v.Value)));
            }
            var ctx = new ControllerContext { HttpContext = http };
            ctx.ValueProviderFactories.Add(new FormValueProviderFactory());
            controller.ControllerContext = ctx;
            controller.Url = new StubUrlHelper(ctx);
            return controller;
        }

        private sealed class StubUrlHelper : Microsoft.AspNetCore.Mvc.IUrlHelper
        {
            public StubUrlHelper(ActionContext ctx) { ActionContext = ctx; }
            public ActionContext ActionContext { get; }
            public string? Action(Microsoft.AspNetCore.Mvc.Routing.UrlActionContext actionContext) => "/stub";
            public string? Content(string? contentPath) => contentPath;
            public bool IsLocalUrl(string? url) => true;
            public string? Link(string? routeName, object? values) => "/stub";
            public string? RouteUrl(Microsoft.AspNetCore.Mvc.Routing.UrlRouteContext routeContext) => "/stub";
        }
    }
}
