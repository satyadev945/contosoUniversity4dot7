using System;
using System.Configuration;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// HttpModule that adds Content-Security-Policy and related security headers to every response.
    /// The policy can be overridden via the CSP_POLICY environment variable or the
    /// "ContentSecurityPolicy" appSetting (12-factor). Complemented by AWS WAF managed rules at the edge.
    /// </summary>
    public class ContentSecurityPolicyModule : IHttpModule
    {
        private const string DefaultPolicy =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
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
            if (response.HeadersWritten)
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

            response.Headers.Remove("Content-Security-Policy");
            response.Headers.Add("Content-Security-Policy", policy);
            response.Headers.Set("X-Content-Type-Options", "nosniff");
            response.Headers.Set("X-Frame-Options", "SAMEORIGIN");
            response.Headers.Set("Referrer-Policy", "strict-origin-when-cross-origin");
        }

        public void Dispose()
        {
        }
    }
}
