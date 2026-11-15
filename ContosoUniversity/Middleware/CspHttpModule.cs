using System;
using System.Configuration;
using System.Web;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// ASP.NET (System.Web) HttpModule that adds Content-Security-Policy and related
    /// security headers to every response (SOC2 / pen-test requirement).
    /// The policy can be overridden without a rebuild through the CSP_POLICY environment
    /// variable or the "ContentSecurityPolicy" appSetting. Set CSP_REPORT_ONLY=true to
    /// roll out in report-only mode. Complements the GCP Cloud Armor WAF policy applied at the edge.
    /// </summary>
    public class CspHttpModule : IHttpModule
    {
        internal const string DefaultPolicy =
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

        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        private static void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            var response = app.Context?.Response;
            if (response == null)
            {
                return;
            }

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
                Environment.GetEnvironmentVariable("CSP_REPORT_ONLY"), "true", StringComparison.OrdinalIgnoreCase);
            var headerName = reportOnly ? "Content-Security-Policy-Report-Only" : "Content-Security-Policy";

            response.Headers.Set(headerName, policy);
            response.Headers.Set("X-Content-Type-Options", "nosniff");
            response.Headers.Set("X-Frame-Options", "DENY");
            response.Headers.Set("Referrer-Policy", "strict-origin-when-cross-origin");
        }

        public void Dispose()
        {
        }
    }
}
