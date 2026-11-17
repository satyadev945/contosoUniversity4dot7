using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// Replaces the IIS "BlockViewHandler" (System.Web.HttpNotFoundHandler) that was configured
    /// in Views/Web.config. Direct requests for anything under /Views return 404.
    /// </summary>
    public class BlockViewAccessMiddleware
    {
        private readonly RequestDelegate _next;

        public BlockViewAccessMiddleware(RequestDelegate next)
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
