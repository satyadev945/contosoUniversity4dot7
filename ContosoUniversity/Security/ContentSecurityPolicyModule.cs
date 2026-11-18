using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Central definition of the Content-Security-Policy (and related security headers).
    /// The policy can be overridden with the CSP_POLICY environment variable (or the
    /// "ContentSecurityPolicy" appSetting) so it can be tuned per environment without a rebuild.
    /// Edge enforcement is provided by AWS WAF managed rules / CloudFront response headers policy.
    /// </summary>
    public static class ContentSecurityPolicy
    {
        public const string HeaderName = "Content-Security-Policy";

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

        public static string GetPolicy()
        {
            var policy = Environment.GetEnvironmentVariable("CSP_POLICY");
            if (string.IsNullOrWhiteSpace(policy))
            {
                policy = ConfigurationManager.AppSettings["ContentSecurityPolicy"];
            }
            return string.IsNullOrWhiteSpace(policy) ? DefaultPolicy : policy;
        }
    }

    /// <summary>
    /// ASP.NET HttpModule (registered in Web.config) that adds the Content-Security-Policy
    /// header to every response.
    /// </summary>
    public class ContentSecurityPolicyModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        private static void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var response = ((HttpApplication)sender).Context?.Response;
            if (response == null || response.HeadersWritten)
            {
                return;
            }

            response.Headers.Remove(ContentSecurityPolicy.HeaderName);
            response.Headers.Add(ContentSecurityPolicy.HeaderName, ContentSecurityPolicy.GetPolicy());
            response.Headers.Remove("X-Content-Type-Options");
            response.Headers.Add("X-Content-Type-Options", "nosniff");
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// ASP.NET Core (Kestrel) equivalent of <see cref="ContentSecurityPolicyModule"/>, used by the
    /// middleware pipeline in MvcApplication.Configure.
    /// </summary>
    public class ContentSecurityPolicyMiddleware
    {
        private readonly RequestDelegate _next;

        public ContentSecurityPolicyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var policy = ContentSecurityPolicy.GetPolicy();
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[ContentSecurityPolicy.HeaderName] = policy;
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                return Task.CompletedTask;
            });
            return _next(context);
        }
    }
}
