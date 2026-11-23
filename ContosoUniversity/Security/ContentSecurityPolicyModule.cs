using System;
using System.Web;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// HttpModule that adds a Content-Security-Policy header (and companion hardening headers)
    /// to every response. The policy can be overridden per environment (e.g. Azure App Service
    /// Application Settings) via the "ContentSecurityPolicy" app setting / environment variable.
    /// </summary>
    public class ContentSecurityPolicyModule : IHttpModule
    {
        public const string HeaderName = "Content-Security-Policy";

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

            var policy = Environment.GetEnvironmentVariable("ContentSecurityPolicy");
            if (string.IsNullOrWhiteSpace(policy))
            {
                policy = System.Configuration.ConfigurationManager.AppSettings["ContentSecurityPolicy"];
            }
            if (string.IsNullOrWhiteSpace(policy))
            {
                policy = DefaultPolicy;
            }

            headers.Remove(HeaderName);
            headers.Add(HeaderName, policy);

            if (headers["X-Content-Type-Options"] == null)
            {
                headers.Add("X-Content-Type-Options", "nosniff");
            }
        }

        public void Dispose()
        {
        }
    }
}
