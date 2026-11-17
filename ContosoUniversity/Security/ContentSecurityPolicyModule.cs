using System;
using System.Configuration;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// HttpModule that emits a Content-Security-Policy header (plus related hardening headers) on every
    /// response. Required for SOC2 / Azure Policy compliance and XSS mitigation in multi-tenant cloud hosting.
    /// The policy can be overridden per environment (Azure App Service application setting
    /// "ContentSecurityPolicy") without redeploying code.
    /// </summary>
    public class ContentSecurityPolicyModule : IHttpModule
    {
        public const string HeaderName = "Content-Security-Policy";
        public const string SettingName = "ContentSecurityPolicy";

        public const string DefaultPolicy =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self' data:; " +
            "connect-src 'self' https://dc.services.visualstudio.com https://*.in.applicationinsights.azure.com; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "frame-ancestors 'self'";

        public static string GetPolicy()
        {
            var configured = Environment.GetEnvironmentVariable(SettingName)
                             ?? ConfigurationManager.AppSettings[SettingName];
            return string.IsNullOrWhiteSpace(configured) ? DefaultPolicy : configured;
        }

        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        private static void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var response = ((HttpApplication)sender).Context.Response;
            if (response.Headers[HeaderName] == null)
            {
                response.Headers.Set(HeaderName, GetPolicy());
            }
            if (response.Headers["X-Content-Type-Options"] == null)
            {
                response.Headers.Set("X-Content-Type-Options", "nosniff");
            }
        }

        public void Dispose()
        {
        }
    }
}
