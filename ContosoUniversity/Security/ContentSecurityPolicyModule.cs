using System;
using System.Configuration;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// HttpModule that adds a Content-Security-Policy (and related hardening) header to every response.
    /// The policy can be overridden without redeploying through the "ContentSecurityPolicy" app setting
    /// (Azure App Service application setting / Key Vault reference / Azure App Configuration).
    /// Set "ContentSecurityPolicyReportOnly" to "true" to emit Content-Security-Policy-Report-Only instead.
    /// </summary>
    public class ContentSecurityPolicyModule : IHttpModule
    {
        public const string SettingName = "ContentSecurityPolicy";
        public const string ReportOnlySettingName = "ContentSecurityPolicyReportOnly";

        public const string DefaultPolicy =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self' https://*.in.applicationinsights.azure.com https://dc.services.visualstudio.com; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "frame-ancestors 'self'";

        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        private static void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            var response = app.Context?.Response;
            if (response == null || response.HeadersWritten)
            {
                return;
            }

            var policy = Environment.GetEnvironmentVariable(SettingName);
            if (string.IsNullOrWhiteSpace(policy))
            {
                policy = ConfigurationManager.AppSettings[SettingName];
            }
            if (string.IsNullOrWhiteSpace(policy))
            {
                policy = DefaultPolicy;
            }

            var reportOnlyValue = Environment.GetEnvironmentVariable(ReportOnlySettingName)
                                  ?? ConfigurationManager.AppSettings[ReportOnlySettingName];
            var reportOnly = string.Equals(reportOnlyValue, "true", StringComparison.OrdinalIgnoreCase);
            var headerName = reportOnly ? "Content-Security-Policy-Report-Only" : "Content-Security-Policy";

            response.Headers.Remove("Content-Security-Policy");
            response.Headers.Remove("Content-Security-Policy-Report-Only");
            response.Headers.Set(headerName, policy);
        }

        public void Dispose()
        {
        }
    }
}
