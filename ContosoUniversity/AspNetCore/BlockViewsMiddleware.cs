using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace ContosoUniversity.AspNetCore
{
    /// <summary>
    /// ASP.NET Core replacement for the IIS "BlockViewHandler" (System.Web.HttpNotFoundHandler)
    /// previously configured in Views/Web.config. Returns 404 for direct requests to /Views.
    /// </summary>
    public class BlockViewsMiddleware
    {
        private readonly RequestDelegate _next;

        public BlockViewsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/Views", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }

            return _next(context);
        }
    }
}
