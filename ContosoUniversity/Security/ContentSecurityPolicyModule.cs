using System;
using System.Configuration;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// HttpModule that adds a Content-Security-Policy header (and companion hardening headers) to every
    /// response, for SOC2 / Azure Policy compliance. The policy can be overridden without redeploying through
    /// the CONTENT_SECURITY_POLICY app setting / environment variable (Azure App Service application setting).
    /// </summary>
    public class ContentSecurityPolicyModule : IHttpModule
    {
        public const string PolicySettingName = "CONTENT_SECURITY_POLICY";

        public const string DefaultPolicy =
            "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; " +
            "font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; " +
            "form-action 'self'; frame-ancestors 'none'";

        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        private static void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            var headers = app.Response.Headers;

            headers.Set("Content-Security-Policy", ResolvePolicy());
            headers.Set("X-Content-Type-Options", "nosniff");
            headers.Set("X-Frame-Options", "DENY");
            headers.Set("Referrer-Policy", "strict-origin-when-cross-origin");
        }

        private static string ResolvePolicy()
        {
            var policy = Environment.GetEnvironmentVariable(PolicySettingName);
            if (string.IsNullOrWhiteSpace(policy))
            {
                policy = ConfigurationManager.AppSettings[PolicySettingName];
            }
            return string.IsNullOrWhiteSpace(policy) ? DefaultPolicy : policy;
        }

        public void Dispose()
        {
        }
    }
}
