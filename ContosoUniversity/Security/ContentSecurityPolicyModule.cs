using System;
using System.Configuration;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// HttpModule that adds Content-Security-Policy (and related hardening) headers to every response.
    /// Registered in Web.config (system.web/httpModules and system.webServer/modules).
    /// The policy can be overridden per environment (e.g. AWS Elastic Beanstalk / ECS environment
    /// variable CSP_POLICY, or the appSettings key "ContentSecurityPolicy") without a redeploy.
    /// Complements AWS WAF managed rules (AWSManagedRulesCommonRuleSet, AWSManagedRulesKnownBadInputsRuleSet)
    /// attached to the ALB / CloudFront distribution fronting the application.
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

        private string _policy;

        public void Init(HttpApplication context)
        {
            _policy = ResolvePolicy();
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        private static string ResolvePolicy()
        {
            var policy = Environment.GetEnvironmentVariable("CSP_POLICY");
            if (string.IsNullOrWhiteSpace(policy))
            {
                policy = ConfigurationManager.AppSettings["ContentSecurityPolicy"];
            }
            return string.IsNullOrWhiteSpace(policy) ? DefaultPolicy : policy;
        }

        private void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            var headers = app.Context?.Response?.Headers;
            if (headers == null)
            {
                return;
            }

            headers.Set("Content-Security-Policy", _policy);
            headers.Set("X-Content-Type-Options", "nosniff");
            headers.Set("X-Frame-Options", "DENY");
            headers.Set("Referrer-Policy", "strict-origin-when-cross-origin");
        }

        public void Dispose()
        {
        }
    }
}
