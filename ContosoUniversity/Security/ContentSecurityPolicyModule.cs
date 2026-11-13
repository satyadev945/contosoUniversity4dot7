using System;
using System.Configuration;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// ASP.NET HttpModule that adds Content-Security-Policy (and related hardening) headers to every response.
    /// Registered in Web.config (system.web/httpModules and system.webServer/modules).
    /// The policy can be overridden per environment through the CSP_POLICY environment variable
    /// or the "Security:ContentSecurityPolicy" appSetting. Complements AWS WAF managed rules at the edge.
    /// </summary>
    public class ContentSecurityPolicyModule : IHttpModule
    {
        public const string DefaultPolicy =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self' data:; " +
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

        private static void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            var headers = app.Context?.Response?.Headers;
            if (headers == null)
            {
                return;
            }

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

        public void Dispose()
        {
        }
    }
}
