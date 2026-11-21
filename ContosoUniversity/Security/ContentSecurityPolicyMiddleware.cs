using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Adds Content-Security-Policy (and related hardening) headers to every response.
    /// Replaces the System.Web HttpModule for the Kestrel / Azure App Service (Linux) pipeline.
    /// The policy can be overridden through the CONTENT_SECURITY_POLICY environment variable.
    /// </summary>
    public class ContentSecurityPolicyMiddleware
    {
        public const string PolicyEnvironmentVariable = "CONTENT_SECURITY_POLICY";

        public const string DefaultPolicy =
            "default-src 'self'; " +
            "script-src 'self' https://js.monitor.azure.com; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self' https://*.applicationinsights.azure.com https://*.in.applicationinsights.azure.com; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "frame-ancestors 'self'";

        private readonly RequestDelegate _next;
        private readonly string _policy;

        public ContentSecurityPolicyMiddleware(RequestDelegate next)
        {
            _next = next;
            var configured = Environment.GetEnvironmentVariable(PolicyEnvironmentVariable);
            _policy = string.IsNullOrWhiteSpace(configured) ? DefaultPolicy : configured;
        }

        public Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["Content-Security-Policy"] = _policy;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "SAMEORIGIN";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                return Task.CompletedTask;
            });

            return _next(context);
        }
    }

    public static class ContentSecurityPolicyExtensions
    {
        public static IApplicationBuilder UseContentSecurityPolicy(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ContentSecurityPolicyMiddleware>();
        }
    }
}
