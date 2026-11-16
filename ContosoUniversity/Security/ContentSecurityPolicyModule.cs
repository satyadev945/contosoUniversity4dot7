using System;
using System.Configuration;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Central definition of the Content-Security-Policy used by the application.
    /// The policy can be overridden without redeploying through the CSP_POLICY environment
    /// variable (e.g. injected from GCP Secret Manager / ConfigMap) or the "Csp:Policy" appSetting.
    /// Set CSP_REPORT_ONLY=true (or "Csp:ReportOnly") to roll out in report-only mode first.
    /// </summary>
    public static class ContentSecurityPolicy
    {
        public const string HeaderName = "Content-Security-Policy";
        public const string ReportOnlyHeaderName = "Content-Security-Policy-Report-Only";

        public const string DefaultPolicy =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "object-src 'none'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

        public static string GetPolicy()
        {
            return Resolve("CSP_POLICY", "Csp:Policy") ?? DefaultPolicy;
        }

        public static bool IsReportOnly()
        {
            var value = Resolve("CSP_REPORT_ONLY", "Csp:ReportOnly");
            bool reportOnly;
            return value != null && bool.TryParse(value, out reportOnly) && reportOnly;
        }

        private static string Resolve(string environmentVariable, string appSettingKey)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariable);
            if (string.IsNullOrWhiteSpace(value))
            {
                value = ConfigurationManager.AppSettings[appSettingKey];
            }
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }

    /// <summary>
    /// ASP.NET HttpModule that adds the Content-Security-Policy header (plus complementary
    /// hardening headers) to every response. Registered in Web.config under both
    /// system.web/httpModules (classic) and system.webServer/modules (integrated).
    /// Works together with a GCP Cloud Armor security policy at the load balancer
    /// for defense-in-depth XSS protection.
    /// </summary>
    public class ContentSecurityPolicyModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        private static void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            var response = app.Context.Response;

            var headerName = ContentSecurityPolicy.IsReportOnly()
                ? ContentSecurityPolicy.ReportOnlyHeaderName
                : ContentSecurityPolicy.HeaderName;

            response.Headers.Remove(headerName);
            response.Headers.Add(headerName, ContentSecurityPolicy.GetPolicy());

            response.Headers.Remove("X-Content-Type-Options");
            response.Headers.Add("X-Content-Type-Options", "nosniff");
        }

        public void Dispose()
        {
        }
    }
}
