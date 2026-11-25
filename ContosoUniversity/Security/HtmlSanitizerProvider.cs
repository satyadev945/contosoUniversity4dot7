using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Ganss.XSS;
using Newtonsoft.Json;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Central HTML sanitization (Ganss.Xss HtmlSanitizer) used instead of Html.Raw / HtmlString.
    /// The allowed tags / attributes / URI schemes are stored centrally in AWS Secrets Manager so they
    /// can be managed once for all tenants. The secret id is read from the
    /// HTML_SANITIZER_SECRET_ID environment variable and must contain JSON such as:
    /// { "allowedTags": ["b","i","p"], "allowedAttributes": ["class"], "allowedSchemes": ["https"] }
    /// When the variable is not set or the secret cannot be read the library's safe defaults are used.
    /// </summary>
    public static class HtmlSanitizerProvider
    {
        private const string SecretIdVariable = "HTML_SANITIZER_SECRET_ID";

        private static readonly Lazy<HtmlSanitizer> Sanitizer = new Lazy<HtmlSanitizer>(CreateSanitizer);

        private class SanitizerSettings
        {
            [JsonProperty("allowedTags")]
            public List<string> AllowedTags { get; set; }

            [JsonProperty("allowedAttributes")]
            public List<string> AllowedAttributes { get; set; }

            [JsonProperty("allowedSchemes")]
            public List<string> AllowedSchemes { get; set; }
        }

        /// <summary>Returns sanitized HTML (disallowed tags/attributes/schemes removed).</summary>
        public static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return Sanitizer.Value.Sanitize(input);
        }

        /// <summary>
        /// Sanitizes a value that will subsequently be rendered through Razor's automatic HTML
        /// encoding (e.g. ViewBag values, text box values). Markup is stripped by the sanitizer and the
        /// remaining entity-escaped text is decoded so it is not double encoded by Razor.
        /// </summary>
        public static string SanitizeForText(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return HttpUtility.HtmlDecode(Sanitizer.Value.Sanitize(input));
        }

        private static HtmlSanitizer CreateSanitizer()
        {
            var sanitizer = new HtmlSanitizer();
            var settings = LoadSettings();
            if (settings == null)
            {
                return sanitizer;
            }

            if (settings.AllowedTags != null && settings.AllowedTags.Count > 0)
            {
                sanitizer.AllowedTags.Clear();
                foreach (var tag in settings.AllowedTags.Where(t => !string.IsNullOrWhiteSpace(t)))
                {
                    sanitizer.AllowedTags.Add(tag.Trim());
                }
            }
            if (settings.AllowedAttributes != null && settings.AllowedAttributes.Count > 0)
            {
                sanitizer.AllowedAttributes.Clear();
                foreach (var attr in settings.AllowedAttributes.Where(a => !string.IsNullOrWhiteSpace(a)))
                {
                    sanitizer.AllowedAttributes.Add(attr.Trim());
                }
            }
            if (settings.AllowedSchemes != null && settings.AllowedSchemes.Count > 0)
            {
                sanitizer.AllowedSchemes.Clear();
                foreach (var scheme in settings.AllowedSchemes.Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    sanitizer.AllowedSchemes.Add(scheme.Trim());
                }
            }
            return sanitizer;
        }

        private static SanitizerSettings LoadSettings()
        {
            var secretId = Environment.GetEnvironmentVariable(SecretIdVariable);
            if (string.IsNullOrWhiteSpace(secretId))
            {
                return null;
            }

            try
            {
                var region = Environment.GetEnvironmentVariable("AWS_REGION")
                             ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
                // Credentials come from the default AWS chain (IAM role for the task / pod / instance).
                using (var client = string.IsNullOrWhiteSpace(region)
                    ? new AmazonSecretsManagerClient()
                    : new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region)))
                {
                    var response = client.GetSecretValue(new GetSecretValueRequest { SecretId = secretId });
                    return JsonConvert.DeserializeObject<SanitizerSettings>(response.SecretString);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Unable to load HTML sanitizer configuration from AWS Secrets Manager, using defaults: " + ex.Message);
                return null;
            }
        }
    }
}
