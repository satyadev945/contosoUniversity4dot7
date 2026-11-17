using System;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// Central definition of the Content-Security-Policy and related security headers.
    /// The CSP can be overridden per environment (e.g. ECS/EKS task definition) via the
    /// CONTENT_SECURITY_POLICY environment variable. Complements AWS WAF managed rules
    /// (AWSManagedRulesCommonRuleSet, AWSManagedRulesKnownBadInputsRuleSet) attached to the ALB/CloudFront.
    /// </summary>
    public static class SecurityHeadersPolicy
    {
        public const string DefaultContentSecurityPolicy =
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

        public static string ContentSecurityPolicy
        {
            get
            {
                var value = Environment.GetEnvironmentVariable("CONTENT_SECURITY_POLICY");
                return string.IsNullOrWhiteSpace(value) ? DefaultContentSecurityPolicy : value;
            }
        }
    }
}
