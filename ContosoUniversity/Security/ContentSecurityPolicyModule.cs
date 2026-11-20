using System;
using System.Configuration;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// HttpModule that adds Content-Security-Policy (and related security) headers to every response.
    /// The policy can be overridden through the CSP_POLICY environment variable or the
    /// "ContentSecurityPolicy" appSetting so it can be tuned per cloud environment (AWS).
    /// Complements AWS WAF managed rules (e.g. AWSManagedRulesCommonRuleSet, AWSManagedRulesKnownBadInputsRuleSet)
    /// attached to the load balancer / CloudFront distribution.
    /// </summary>
    public class ContentSecurityPolicyModule : IHttpModule
    {
        private const string DefaultPolicy =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "object-src 'none'; " +
            "frame-ancestors 'self'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        private static void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            var response = app.Context.Response;

            if (response.Headers["Content-Security-Policy"] == null)
            {
                response.Headers.Set("Content-Security-Policy", GetPolicy());
            }
            if (response.Headers["X-Content-Type-Options"] == null)
            {
                response.Headers.Set("X-Content-Type-Options", "nosniff");
            }
            if (response.Headers["X-Frame-Options"] == null)
            {
                response.Headers.Set("X-Frame-Options", "SAMEORIGIN");
            }
            if (response.Headers["Referrer-Policy"] == null)
            {
                response.Headers.Set("Referrer-Policy", "strict-origin-when-cross-origin");
            }
        }

        private static string GetPolicy()
        {
            var policy = Environment.GetEnvironmentVariable("CSP_POLICY");
            if (string.IsNullOrWhiteSpace(policy))
            {
                policy = ConfigurationManager.AppSettings["ContentSecurityPolicy"];
            }
            return string.IsNullOrWhiteSpace(policy) ? DefaultPolicy : policy;
        }

        public void Dispose()
        {
        }
    }
}
