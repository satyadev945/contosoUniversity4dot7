using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// ASP.NET Core (Kestrel) counterpart of ContentSecurityPolicyModule so the same headers
    /// are emitted when the app is hosted behind an AWS ALB protected by AWS WAF.
    /// Policy can be overridden with the CSP_POLICY environment variable.
    /// </summary>
    public class ContentSecurityPolicyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _policy;

        public ContentSecurityPolicyMiddleware(RequestDelegate next)
        {
            _next = next;
            var fromEnv = Environment.GetEnvironmentVariable("CSP_POLICY");
            _policy = string.IsNullOrWhiteSpace(fromEnv)
                ? ContosoUniversity.Security.ContentSecurityPolicyModule.DefaultPolicy
                : fromEnv;
        }

        public Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["Content-Security-Policy"] = _policy;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                return Task.CompletedTask;
            });

            return _next(context);
        }
    }
}
