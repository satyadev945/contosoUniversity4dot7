using System;
using System.Configuration;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// HttpModule that adds Content-Security-Policy (and companion security) headers to every response.
    /// Registered in Web.config (system.web/httpModules and system.webServer/modules).
    /// The policy can be overridden per environment via the CSP_POLICY environment variable
    /// or the "Security:ContentSecurityPolicy" appSetting. Intended to be used together with
    /// AWS WAF managed rules (e.g. AWSManagedRulesCommonRuleSet) attached to the ALB / CloudFront distribution.
    /// </summary>
    public class ContentSecurityPolicyModule : IHttpModule
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

        private static readonly string Policy = ResolvePolicy();

        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        public void Dispose()
        {
        }

        private static void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var app = sender as HttpApplication;
            if (app == null || app.Context == null || app.Context.Response == null)
            {
                return;
            }

            var headers = app.Context.Response.Headers;
            headers.Set("Content-Security-Policy", Policy);
            headers.Set("X-Content-Type-Options", "nosniff");
            headers.Set("X-Frame-Options", "DENY");
            headers.Set("Referrer-Policy", "strict-origin-when-cross-origin");
        }

        private static string ResolvePolicy()
        {
            var fromEnv = Environment.GetEnvironmentVariable("CSP_POLICY");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv;
            }

            var fromConfig = ConfigurationManager.AppSettings["Security:ContentSecurityPolicy"];
            return string.IsNullOrWhiteSpace(fromConfig) ? DefaultPolicy : fromConfig;
        }
    }
}
