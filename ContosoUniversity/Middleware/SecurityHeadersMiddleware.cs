using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// Adds the Content-Security-Policy response header (and companion hardening headers) to every
    /// response. This is the Kestrel/ASP.NET Core equivalent of the CSP HttpModule/customHeaders
    /// declared in Web.config. The policy can be overridden with the CONTENT_SECURITY_POLICY
    /// environment variable so it can be tuned per environment without a rebuild. AWS WAF managed
    /// rules (e.g. AWSManagedRulesCommonRuleSet) are applied at the ALB/CloudFront layer in
    /// addition to this header.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        public const string DefaultPolicy =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "frame-ancestors 'none'";

        private readonly RequestDelegate _next;
        private readonly string _policy;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
            var configured = Environment.GetEnvironmentVariable("CONTENT_SECURITY_POLICY");
            _policy = string.IsNullOrWhiteSpace(configured) ? DefaultPolicy : configured;
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

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
