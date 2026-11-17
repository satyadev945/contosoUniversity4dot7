using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// Replaces the IIS "BlockViewHandler" (System.Web.HttpNotFoundHandler) that was configured in
    /// Views/Web.config. Any direct request to the Views folder returns 404 so that view files
    /// are never served by Kestrel's static file handling.
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
