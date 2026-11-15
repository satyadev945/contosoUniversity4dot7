using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// ASP.NET Core (Kestrel / GKE) equivalent of CspHttpModule. Adds Content-Security-Policy
    /// and related security headers to every response. Override the policy via the CSP_POLICY
    /// environment variable; set CSP_REPORT_ONLY=true for report-only rollout.
    /// </summary>
    public class CspHeadersMiddleware
    {
        private const string DefaultPolicy =
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; " +
            "base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

        private readonly RequestDelegate _next;
        private readonly string _policy;
        private readonly string _headerName;

        public CspHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
            var policy = Environment.GetEnvironmentVariable("CSP_POLICY");
            _policy = string.IsNullOrWhiteSpace(policy) ? DefaultPolicy : policy;
            var reportOnly = string.Equals(
                Environment.GetEnvironmentVariable("CSP_REPORT_ONLY"), "true", StringComparison.OrdinalIgnoreCase);
            _headerName = reportOnly ? "Content-Security-Policy-Report-Only" : "Content-Security-Policy";
        }

        public Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers[_headerName] = _policy;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                return Task.CompletedTask;
            });
            return _next(context);
        }
    }
}
