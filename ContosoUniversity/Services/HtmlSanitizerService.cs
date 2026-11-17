using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Ganss.XSS;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Shape of the sanitization policy stored as a JSON secret in Azure Key Vault, e.g.
    /// {"allowedTags":["b","i","em","strong"],"allowedAttributes":["class"],"allowedSchemes":["https","mailto"]}
    /// </summary>
    public class HtmlSanitizationPolicy
    {
        [JsonProperty("allowedTags")]
        public List<string> AllowedTags { get; set; } = new List<string>();

        [JsonProperty("allowedAttributes")]
        public List<string> AllowedAttributes { get; set; } = new List<string>();

        [JsonProperty("allowedSchemes")]
        public List<string> AllowedSchemes { get; set; } = new List<string> { "https" };
    }

    public interface IHtmlSanitizerService
    {
        /// <summary>Sanitizes HTML according to the centrally managed policy.</summary>
        string Sanitize(string input);

        /// <summary>
        /// Sanitizes a value that will be rendered as plain text. The result is not HTML-encoded so that
        /// Razor's automatic encoding is applied exactly once at render time.
        /// </summary>
        string SanitizeText(string input);
    }

    /// <summary>
    /// Wraps Ganss.Xss.HtmlSanitizer. The allowed tags/attributes policy is loaded from Azure Key Vault
    /// (vault URI from AZURE_KEYVAULT_URI, secret name from HTML_SANITIZER_POLICY_SECRET_NAME,
    /// default "HtmlSanitizerPolicy"). If Key Vault is unavailable, a restrictive policy (no tags) is used.
    /// </summary>
    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        private const string DefaultSecretName = "HtmlSanitizerPolicy";
        private static readonly Lazy<HtmlSanitizer> SharedSanitizer = new Lazy<HtmlSanitizer>(CreateSanitizer);

        public static IHtmlSanitizerService Instance { get; } = new HtmlSanitizerService();

        public string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return SharedSanitizer.Value.Sanitize(input);
        }

        public string SanitizeText(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return HttpUtility.HtmlDecode(Sanitize(input));
        }

        private static HtmlSanitizer CreateSanitizer()
        {
            var policy = LoadPolicy();
            var sanitizer = new HtmlSanitizer();

            sanitizer.AllowedTags.Clear();
            foreach (var tag in policy.AllowedTags.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                sanitizer.AllowedTags.Add(tag);
            }

            sanitizer.AllowedAttributes.Clear();
            foreach (var attr in policy.AllowedAttributes.Where(a => !string.IsNullOrWhiteSpace(a)))
            {
                sanitizer.AllowedAttributes.Add(attr);
            }

            sanitizer.AllowedSchemes.Clear();
            foreach (var scheme in policy.AllowedSchemes.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                sanitizer.AllowedSchemes.Add(scheme);
            }

            return sanitizer;
        }

        private static HtmlSanitizationPolicy LoadPolicy()
        {
            var vaultUri = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URI");
            if (string.IsNullOrWhiteSpace(vaultUri))
            {
                return new HtmlSanitizationPolicy();
            }

            try
            {
                var secretName = Environment.GetEnvironmentVariable("HTML_SANITIZER_POLICY_SECRET_NAME");
                if (string.IsNullOrWhiteSpace(secretName))
                {
                    secretName = DefaultSecretName;
                }

                var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
                var secret = client.GetSecret(secretName);
                var policy = JsonConvert.DeserializeObject<HtmlSanitizationPolicy>(secret.Value.Value);
                return policy ?? new HtmlSanitizationPolicy();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Failed to load HTML sanitizer policy from Key Vault: " + ex.Message);
                return new HtmlSanitizationPolicy();
            }
        }
    }
}
