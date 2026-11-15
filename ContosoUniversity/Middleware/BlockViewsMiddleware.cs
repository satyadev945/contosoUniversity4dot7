using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// ASP.NET Core replacement for the IIS "BlockViewHandler" (System.Web.HttpNotFoundHandler)
    /// that was declared in Views/Web.config under system.webServer/handlers.
    /// Returns 404 for any direct request to the Views folder.
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
