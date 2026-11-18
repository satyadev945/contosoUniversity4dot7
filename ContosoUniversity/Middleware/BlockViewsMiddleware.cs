using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// Replaces the IIS "BlockViewHandler" (System.Web.HttpNotFoundHandler) that was configured in
    /// Views/Web.config. Any direct request to the /Views folder returns 404.
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
