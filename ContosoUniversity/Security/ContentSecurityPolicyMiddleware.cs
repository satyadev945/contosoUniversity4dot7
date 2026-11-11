using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Adds Content-Security-Policy (and related hardening) response headers to every response.
    /// This is the ASP.NET Core equivalent of the Web Forms/MVC HttpModule registered in web.config.
    /// The policy can be overridden per environment through the CONTENT_SECURITY_POLICY
    /// environment variable (e.g. in ECS task definition / Elastic Beanstalk environment properties).
    /// It is complemented at the edge by AWS WAF managed rule groups (e.g. AWSManagedRulesCommonRuleSet,
    /// AWSManagedRulesKnownBadInputsRuleSet) attached to the ALB / CloudFront distribution.
    /// </summary>
    public class ContentSecurityPolicyMiddleware
    {
        public const string PolicyEnvironmentVariable = "CONTENT_SECURITY_POLICY";

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

        private readonly RequestDelegate _next;
        private readonly string _policy;

        public ContentSecurityPolicyMiddleware(RequestDelegate next)
        {
            _next = next;
            var configured = Environment.GetEnvironmentVariable(PolicyEnvironmentVariable);
            _policy = string.IsNullOrWhiteSpace(configured) ? DefaultPolicy : configured;
        }

        public Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["Content-Security-Policy"] = _policy;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                return Task.CompletedTask;
            });

            return _next(context);
        }
    }
}
