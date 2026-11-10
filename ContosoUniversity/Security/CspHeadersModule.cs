using System;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// HttpModule that adds Content-Security-Policy (and related hardening) headers to every response.
    /// The policy can be overridden per environment (e.g. Azure App Service application setting
    /// CSP_POLICY) without a code change; otherwise a strict default is used.
    /// </summary>
    public class CspHeadersModule : IHttpModule
    {
        internal const string DefaultPolicy =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self' https://dc.services.visualstudio.com https://*.in.applicationinsights.azure.com; " +
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
            var response = ((HttpApplication)sender).Context?.Response;
            if (response == null)
            {
                return;
            }

            var policy = Environment.GetEnvironmentVariable("CSP_POLICY");
            if (string.IsNullOrWhiteSpace(policy))
            {
                policy = DefaultPolicy;
            }

            // Avoid duplicates when web.config customHeaders already supplied the header.
            response.Headers.Remove("Content-Security-Policy");
            response.Headers.Set("Content-Security-Policy", policy);
        }

        public void Dispose()
        {
        }
    }
}
