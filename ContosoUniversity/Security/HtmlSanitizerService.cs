using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Ganss.XSS;
using Newtonsoft.Json;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Shape of the sanitization policy stored in AWS Secrets Manager, e.g.
    /// {"allowedTags":["b","i","p"],"allowedAttributes":["class"],"allowedSchemes":["http","https"],"allowedCssProperties":[]}
    /// </summary>
    public class HtmlSanitizerConfig
    {
        [JsonProperty("allowedTags")]
        public List<string> AllowedTags { get; set; }

        [JsonProperty("allowedAttributes")]
        public List<string> AllowedAttributes { get; set; }

        [JsonProperty("allowedSchemes")]
        public List<string> AllowedSchemes { get; set; }

        [JsonProperty("allowedCssProperties")]
        public List<string> AllowedCssProperties { get; set; }
    }

    /// <summary>
    /// Central HTML sanitization (Ganss.Xss HtmlSanitizer). The allowed tags / attributes policy is
    /// loaded from AWS Secrets Manager (secret id in HTML_SANITIZER_SECRET_ID, region in AWS_REGION /
    /// AWS_DEFAULT_REGION) so it can be managed centrally across tenants. If no secret is configured
    /// or it cannot be read, the most restrictive policy is used (all markup stripped).
    /// </summary>
    public static class HtmlSanitizerService
    {
        private static readonly object SyncRoot = new object();
        private static HtmlSanitizer _sanitizer;

        /// <summary>Sanitizes untrusted HTML/text and returns safe markup (never null).</summary>
        public static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input ?? string.Empty;
            }
            return GetSanitizer().Sanitize(input);
        }

        /// <summary>Sanitizes the value if it is a string; other types are returned unchanged.</summary>
        public static object SanitizeObject(object value)
        {
            var text = value as string;
            return text == null ? value : Sanitize(text);
        }

        /// <summary>Forces the policy to be re-read from Secrets Manager on next use.</summary>
        public static void Reload()
        {
            lock (SyncRoot)
            {
                _sanitizer = null;
            }
        }

        private static HtmlSanitizer GetSanitizer()
        {
            var current = _sanitizer;
            if (current != null)
            {
                return current;
            }
            lock (SyncRoot)
            {
                if (_sanitizer == null)
                {
                    _sanitizer = BuildSanitizer(LoadConfig());
                }
                return _sanitizer;
            }
        }

        private static HtmlSanitizer BuildSanitizer(HtmlSanitizerConfig config)
        {
            var sanitizer = new HtmlSanitizer();
            // Restrictive by default: nothing allowed unless the centrally managed policy allows it.
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedCssProperties.Clear();
            sanitizer.AllowedSchemes.Clear();
            sanitizer.AllowedSchemes.Add("https");

            if (config == null)
            {
                return sanitizer;
            }

            Apply(sanitizer.AllowedTags, config.AllowedTags);
            Apply(sanitizer.AllowedAttributes, config.AllowedAttributes);
            Apply(sanitizer.AllowedCssProperties, config.AllowedCssProperties);
            if (config.AllowedSchemes != null && config.AllowedSchemes.Count > 0)
            {
                sanitizer.AllowedSchemes.Clear();
                Apply(sanitizer.AllowedSchemes, config.AllowedSchemes);
            }
            return sanitizer;
        }

        private static void Apply(ISet<string> target, IEnumerable<string> values)
        {
            if (values == null)
            {
                return;
            }
            foreach (var v in values.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                target.Add(v.Trim());
            }
        }

        private static HtmlSanitizerConfig LoadConfig()
        {
            var secretId = Environment.GetEnvironmentVariable("HTML_SANITIZER_SECRET_ID");
            if (string.IsNullOrWhiteSpace(secretId))
            {
                return null;
            }

            try
            {
                var region = Environment.GetEnvironmentVariable("AWS_REGION")
                             ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
                // Credentials come from the default chain (IAM role / task role / env) - never hardcoded.
                using (var client = string.IsNullOrWhiteSpace(region)
                    ? new AmazonSecretsManagerClient()
                    : new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region)))
                {
                    var response = client.GetSecretValue(new GetSecretValueRequest { SecretId = secretId });
                    return string.IsNullOrWhiteSpace(response.SecretString)
                        ? null
                        : JsonConvert.DeserializeObject<HtmlSanitizerConfig>(response.SecretString);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("{\"level\":\"error\",\"message\":\"Unable to load HTML sanitizer policy from Secrets Manager; using restrictive defaults\",\"error\":\"" +
                                        ex.GetType().Name + "\"}");
                return null;
            }
        }
    }
}
