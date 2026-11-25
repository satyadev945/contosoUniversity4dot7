using System;
using System.Configuration;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// HttpModule that adds Content-Security-Policy and related security headers to every response.
    /// The policy can be overridden per environment through the CSP_POLICY environment variable
    /// or the "ContentSecurityPolicy" appSetting (12-factor configuration). Set CSP_REPORT_ONLY=true
    /// to emit Content-Security-Policy-Report-Only while tuning the policy.
    /// Complements AWS WAF managed rules applied at the edge (ALB / CloudFront).
    /// </summary>
    public class SecurityHeadersModule : IHttpModule
    {
        private const string DefaultPolicy =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self' data:; " +
            "connect-src 'self'; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "frame-ancestors 'self'";

        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        private static void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            var response = app.Context.Response;

            var policy = Environment.GetEnvironmentVariable("CSP_POLICY");
            if (string.IsNullOrWhiteSpace(policy))
            {
                policy = ConfigurationManager.AppSettings["ContentSecurityPolicy"];
            }
            if (string.IsNullOrWhiteSpace(policy))
            {
                policy = DefaultPolicy;
            }

            var reportOnly = string.Equals(
                Environment.GetEnvironmentVariable("CSP_REPORT_ONLY"), "true",
                StringComparison.OrdinalIgnoreCase);
            var headerName = reportOnly ? "Content-Security-Policy-Report-Only" : "Content-Security-Policy";

            response.Headers.Remove(headerName);
            response.Headers.Add(headerName, policy);

            response.Headers.Remove("X-Content-Type-Options");
            response.Headers.Add("X-Content-Type-Options", "nosniff");
            response.Headers.Remove("X-Frame-Options");
            response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
            response.Headers.Remove("Referrer-Policy");
            response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        }

        public void Dispose()
        {
        }
    }
}
