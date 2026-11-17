using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// Replaces the ASP.NET Web Forms/IIS HttpModule + web.config customHeaders approach for
    /// emitting Content-Security-Policy (and related hardening) headers. Runs on Kestrel (GKE /
    /// Cloud Run) and complements the GCP Cloud Armor WAF policy applied at the load balancer
    /// (defense-in-depth XSS protection for SOC2 compliance).
    ///
    /// The policy can be overridden without redeploying through the CSP_POLICY environment
    /// variable (e.g. supplied from a Kubernetes ConfigMap or Secret Manager).
    /// Set CSP_REPORT_ONLY=true to emit Content-Security-Policy-Report-Only while tuning.
    /// </summary>
    public class ContentSecurityPolicyMiddleware
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
            "frame-ancestors 'self'";

        private readonly RequestDelegate _next;
        private readonly string _policy;
        private readonly string _headerName;

        public ContentSecurityPolicyMiddleware(RequestDelegate next)
        {
            _next = next;

            var configured = Environment.GetEnvironmentVariable("CSP_POLICY");
            _policy = string.IsNullOrWhiteSpace(configured) ? DefaultPolicy : configured;

            var reportOnly = string.Equals(
                Environment.GetEnvironmentVariable("CSP_REPORT_ONLY"), "true",
                StringComparison.OrdinalIgnoreCase);
            _headerName = reportOnly ? "Content-Security-Policy-Report-Only" : "Content-Security-Policy";
        }

        public Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers[_headerName] = _policy;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                return Task.CompletedTask;
            });

            return _next(context);
        }
    }
}
