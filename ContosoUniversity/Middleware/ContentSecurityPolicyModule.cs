using System;
using System.Web;

namespace ContosoUniversity.Middleware
{
    /// <summary>
    /// ASP.NET (System.Web) HttpModule that adds Content-Security-Policy and related security
    /// headers to every response. Registered in Web.config (system.web/httpModules and
    /// system.webServer/modules).
    /// </summary>
    public class ContentSecurityPolicyModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        private static void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var response = ((HttpApplication)sender).Context.Response;
            response.Headers.Set("Content-Security-Policy", SecurityHeadersPolicy.ContentSecurityPolicy);
            response.Headers.Set("X-Content-Type-Options", "nosniff");
            response.Headers.Set("X-Frame-Options", "DENY");
            response.Headers.Set("Referrer-Policy", "strict-origin-when-cross-origin");
        }

        public void Dispose()
        {
        }
    }
}
