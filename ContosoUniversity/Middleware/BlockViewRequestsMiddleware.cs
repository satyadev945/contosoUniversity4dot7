using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// ASP.NET Core replacement for the IIS "BlockViewHandler" (System.Web.HttpNotFoundHandler)
    /// that was registered in Views/Web.config. Direct requests to the Views folder return 404.
    /// </summary>
    public class BlockViewRequestsMiddleware
    {
        private readonly RequestDelegate _next;

        public BlockViewRequestsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path;
            if (path.StartsWithSegments("/Views", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }

            return _next(context);
        }
    }
}
