using System;
using System.Web;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Adds a Content-Security-Policy header to every response (SOC2 / Azure Policy compliance).
    /// The policy can be overridden with the CSP_POLICY environment variable (e.g. App Service app setting).
    /// </summary>
    public class CspHeaderModule : IHttpModule
    {
        private const string DefaultPolicy =
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; " +
            "base-uri 'self'; form-action 'self'; frame-ancestors 'self'";

        public static string GetPolicy()
        {
            var policy = Environment.GetEnvironmentVariable("CSP_POLICY");
            return string.IsNullOrWhiteSpace(policy) ? DefaultPolicy : policy;
        }

        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += (sender, e) =>
            {
                var response = ((HttpApplication)sender).Context.Response;
                if (response.Headers["Content-Security-Policy"] == null)
                {
                    response.Headers.Set("Content-Security-Policy", GetPolicy());
                }
            };
        }

        public void Dispose()
        {
        }
    }
}
