using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// ASP.NET Core replacement for the IIS "BlockViewHandler" (System.Web.HttpNotFoundHandler)
    /// previously declared in Views/Web.config. Direct requests to view files return 404.
    /// </summary>
    public class BlockViewMiddleware
    {
        private readonly RequestDelegate _next;

        public BlockViewMiddleware(RequestDelegate next)
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

    public static class BlockViewMiddlewareExtensions
    {
        public static IApplicationBuilder UseBlockViewHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<BlockViewMiddleware>();
        }
    }
}
