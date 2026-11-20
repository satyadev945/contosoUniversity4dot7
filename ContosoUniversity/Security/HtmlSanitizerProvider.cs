using System;
using System.Diagnostics;
using System.Net;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Ganss.XSS;
using Newtonsoft.Json;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Central HTML sanitization using Ganss.Xss.HtmlSanitizer. The allow-list of tags,
    /// attributes and URL schemes is read from AWS Secrets Manager so it can be managed
    /// centrally across tenants. If the secret is not configured/available, a strict
    /// default (no tags, no attributes, no schemes) is used (fail closed).
    /// </summary>
    public static class HtmlSanitizerProvider
    {
        public const string SecretIdEnvVar = "SANITIZER_CONFIG_SECRET_ID";

        private static readonly object SyncRoot = new object();
        private static HtmlSanitizer _sanitizer;

        private static HtmlSanitizer GetSanitizer()
        {
            if (_sanitizer != null)
            {
                return _sanitizer;
            }

            lock (SyncRoot)
            {
                if (_sanitizer == null)
                {
                    _sanitizer = Build(LoadConfig());
                }
                return _sanitizer;
            }
        }

        private static SanitizerConfig LoadConfig()
        {
            var secretId = Environment.GetEnvironmentVariable(SecretIdEnvVar);
            if (string.IsNullOrWhiteSpace(secretId))
            {
                return new SanitizerConfig();
            }

            try
            {
                var region = Environment.GetEnvironmentVariable("AWS_REGION")
                             ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
                using (var client = string.IsNullOrWhiteSpace(region)
                    ? new AmazonSecretsManagerClient()
                    : new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region)))
                {
                    var response = client.GetSecretValue(new GetSecretValueRequest { SecretId = secretId });
                    var config = JsonConvert.DeserializeObject<SanitizerConfig>(response.SecretString);
                    return config ?? new SanitizerConfig();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unable to load HTML sanitizer configuration from AWS Secrets Manager: " + ex.Message);
                return new SanitizerConfig();
            }
        }

        private static HtmlSanitizer Build(SanitizerConfig config)
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedSchemes.Clear();

            if (config.AllowedTags != null)
            {
                foreach (var tag in config.AllowedTags) sanitizer.AllowedTags.Add(tag);
            }
            if (config.AllowedAttributes != null)
            {
                foreach (var attr in config.AllowedAttributes) sanitizer.AllowedAttributes.Add(attr);
            }
            if (config.AllowedSchemes != null)
            {
                foreach (var scheme in config.AllowedSchemes) sanitizer.AllowedSchemes.Add(scheme);
            }
            return sanitizer;
        }

        /// <summary>Returns sanitized HTML for the given input.</summary>
        public static string Sanitize(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }
            return GetSanitizer().Sanitize(html);
        }

        /// <summary>
        /// Sanitizes the input and returns text that is safe to emit through Razor's
        /// automatic HTML encoding (avoids double-encoding of entities).
        /// </summary>
        public static string SanitizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            return WebUtility.HtmlDecode(GetSanitizer().Sanitize(text));
        }
    }
}
