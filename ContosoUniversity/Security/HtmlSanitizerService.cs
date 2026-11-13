using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Ganss.Xss;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Central HTML sanitization (Ganss.Xss HtmlSanitizer). Replaces any use of Html.Raw / HtmlString
    /// for user-influenced content. The allowed tags / attributes / URI schemes are loaded from
    /// AWS Secrets Manager (secret id taken from the HTML_SANITIZER_SECRET_ID environment variable)
    /// so they can be managed centrally across tenants. If no secret is configured or it cannot be
    /// read, HtmlSanitizer's secure defaults are used.
    ///
    /// Expected secret JSON:
    /// { "AllowedTags": ["b","i","p"], "AllowedAttributes": ["class"], "AllowedSchemes": ["https"] }
    /// </summary>
    public interface IHtmlSanitizerService
    {
        string Sanitize(string html);
    }

    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        public const string SecretIdEnvironmentVariable = "HTML_SANITIZER_SECRET_ID";

        private static readonly Lazy<HtmlSanitizerService> _instance =
            new Lazy<HtmlSanitizerService>(() => new HtmlSanitizerService());

        private readonly HtmlSanitizer _sanitizer;

        /// <summary>Shared instance for code paths without dependency injection (e.g. MVC controllers).</summary>
        public static HtmlSanitizerService Instance
        {
            get { return _instance.Value; }
        }

        public HtmlSanitizerService()
        {
            _sanitizer = new HtmlSanitizer();
            ApplyConfiguration(LoadConfiguration());
        }

        public string Sanitize(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }
            return _sanitizer.Sanitize(html);
        }

        private void ApplyConfiguration(SanitizerConfiguration config)
        {
            if (config == null)
            {
                return;
            }
            if (config.AllowedTags != null)
            {
                _sanitizer.AllowedTags.Clear();
                foreach (var t in config.AllowedTags) _sanitizer.AllowedTags.Add(t);
            }
            if (config.AllowedAttributes != null)
            {
                _sanitizer.AllowedAttributes.Clear();
                foreach (var a in config.AllowedAttributes) _sanitizer.AllowedAttributes.Add(a);
            }
            if (config.AllowedSchemes != null)
            {
                _sanitizer.AllowedSchemes.Clear();
                foreach (var s in config.AllowedSchemes) _sanitizer.AllowedSchemes.Add(s);
            }
        }

        private static SanitizerConfiguration LoadConfiguration()
        {
            var secretId = Environment.GetEnvironmentVariable(SecretIdEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(secretId))
            {
                return null;
            }

            try
            {
                // Region and credentials come from the default AWS chain (env vars / IAM role).
                using (var client = new AmazonSecretsManagerClient())
                {
                    var response = client.GetSecretValue(new GetSecretValueRequest { SecretId = secretId });
                    if (string.IsNullOrEmpty(response.SecretString))
                    {
                        return null;
                    }
                    return new JavaScriptSerializer().Deserialize<SanitizerConfiguration>(response.SecretString);
                }
            }
            catch (Exception ex)
            {
                // Fail closed to HtmlSanitizer's secure defaults.
                Console.Error.WriteLine("{\"level\":\"warn\",\"message\":\"Unable to load HTML sanitizer configuration from AWS Secrets Manager; using defaults\",\"error\":\"" +
                    ex.GetType().Name + "\"}");
                return null;
            }
        }

        private class SanitizerConfiguration
        {
            public List<string> AllowedTags { get; set; }
            public List<string> AllowedAttributes { get; set; }
            public List<string> AllowedSchemes { get; set; }
        }
    }
}
