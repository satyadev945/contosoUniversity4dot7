using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// ASP.NET Core middleware replacing the IIS BlockViewHandler
    /// (System.Web.HttpNotFoundHandler) configured in Views/Web.config.
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
