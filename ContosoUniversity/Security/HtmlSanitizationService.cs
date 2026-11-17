using System;
using System.Net;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Ganss.Xss;
using Newtonsoft.Json;

namespace ContosoUniversity.Security
{
    public interface IHtmlSanitizationService
    {
        /// <summary>Returns sanitized HTML (only allow-listed tags/attributes remain).</summary>
        string Sanitize(string html);

        /// <summary>Strips all markup and returns plain text that is safe to pass to Razor (which HTML-encodes on output).</summary>
        string SanitizeToText(string value);
    }

    /// <summary>
    /// Ganss.Xss based sanitizer. Allow-list configuration is loaded from AWS Secrets Manager
    /// (HTML_SANITIZER_SECRET_ID, region from AWS_REGION / AWS_DEFAULT_REGION). If the secret is not
    /// configured or unavailable, a restrictive built-in default is used.
    /// </summary>
    public class HtmlSanitizationService : IHtmlSanitizationService
    {
        private static readonly Lazy<HtmlSanitizationService> _default =
            new Lazy<HtmlSanitizationService>(() => new HtmlSanitizationService(LoadConfig()));

        public static IHtmlSanitizationService Default => _default.Value;

        private readonly HtmlSanitizer _sanitizer;
        private readonly HtmlSanitizer _textSanitizer;

        public HtmlSanitizationService(HtmlSanitizationConfig config)
        {
            config = config ?? new HtmlSanitizationConfig();

            _sanitizer = new HtmlSanitizer();
            _sanitizer.AllowedTags.Clear();
            foreach (var t in config.AllowedTags) _sanitizer.AllowedTags.Add(t);
            _sanitizer.AllowedAttributes.Clear();
            foreach (var a in config.AllowedAttributes) _sanitizer.AllowedAttributes.Add(a);
            _sanitizer.AllowedSchemes.Clear();
            foreach (var s in config.AllowedSchemes) _sanitizer.AllowedSchemes.Add(s);

            _textSanitizer = new HtmlSanitizer();
            _textSanitizer.AllowedTags.Clear();
            _textSanitizer.AllowedAttributes.Clear();
        }

        public string Sanitize(string html)
        {
            return string.IsNullOrEmpty(html) ? html : _sanitizer.Sanitize(html);
        }

        public string SanitizeToText(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            // Remove all markup, then decode entities so Razor's own encoding is not applied twice.
            return WebUtility.HtmlDecode(_textSanitizer.Sanitize(value));
        }

        private static HtmlSanitizationConfig LoadConfig()
        {
            var secretId = Environment.GetEnvironmentVariable("HTML_SANITIZER_SECRET_ID");
            if (string.IsNullOrWhiteSpace(secretId))
            {
                return new HtmlSanitizationConfig();
            }

            try
            {
                var region = Environment.GetEnvironmentVariable("AWS_REGION")
                             ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
                using (var client = string.IsNullOrWhiteSpace(region)
                    ? new AmazonSecretsManagerClient()
                    : new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region)))
                {
                    var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId })
                        .GetAwaiter().GetResult();
                    var cfg = JsonConvert.DeserializeObject<HtmlSanitizationConfig>(response.SecretString);
                    return cfg ?? new HtmlSanitizationConfig();
                }
            }
            catch (Exception)
            {
                // Fall back to the restrictive defaults rather than failing open.
                return new HtmlSanitizationConfig();
            }
        }
    }
}
