using System;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// HttpModule that adds Content-Security-Policy (and complementary hardening) headers to every response.
    /// Registered in Web.config (system.web/httpModules and system.webServer/modules).
    /// The policy can be overridden through the CONTENT_SECURITY_POLICY environment variable
    /// (e.g. supplied by AWS Systems Manager / Secrets Manager); it is meant to be used together with
    /// AWS WAF managed rule groups (AWSManagedRulesCommonRuleSet, AWSManagedRulesKnownBadInputsRuleSet)
    /// attached to the fronting ALB / CloudFront distribution.
    /// </summary>
    public class ContentSecurityPolicyModule : IHttpModule
    {
        public const string PolicyVariable = "CONTENT_SECURITY_POLICY";

        public const string DefaultPolicy =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "frame-ancestors 'none'";

        public static string GetPolicy()
        {
            var configured = Environment.GetEnvironmentVariable(PolicyVariable);
            return string.IsNullOrWhiteSpace(configured) ? DefaultPolicy : configured;
        }

        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        private static void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var response = ((HttpApplication)sender).Context.Response;
            response.Headers.Remove("Content-Security-Policy");
            response.Headers.Add("Content-Security-Policy", GetPolicy());
            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.Headers["X-Frame-Options"] = "DENY";
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        }

        public void Dispose()
        {
        }
    }
}
