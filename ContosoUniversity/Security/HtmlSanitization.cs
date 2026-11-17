using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Ganss.Xss;
using Newtonsoft.Json;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Allowed tags/attributes policy for <see cref="HtmlSanitizer"/>. The policy is stored as a JSON
    /// secret in Azure Key Vault so it can be managed centrally for multi-tenant Azure SaaS deployments.
    /// Example secret value:
    /// {"allowedTags":["b","i","em","strong","p","br"],"allowedAttributes":["class"],"allowedSchemes":["https"],"allowedCssProperties":[]}
    /// </summary>
    public class HtmlSanitizationPolicy
    {
        [JsonProperty("allowedTags")]
        public List<string> AllowedTags { get; set; }

        [JsonProperty("allowedAttributes")]
        public List<string> AllowedAttributes { get; set; }

        [JsonProperty("allowedSchemes")]
        public List<string> AllowedSchemes { get; set; }

        [JsonProperty("allowedCssProperties")]
        public List<string> AllowedCssProperties { get; set; }

        public static HtmlSanitizationPolicy Default()
        {
            return new HtmlSanitizationPolicy
            {
                AllowedTags = new List<string> { "b", "i", "em", "strong", "p", "br", "span" },
                AllowedAttributes = new List<string> { "class" },
                AllowedSchemes = new List<string> { "https" },
                AllowedCssProperties = new List<string>()
            };
        }
    }

    /// <summary>
    /// Central HTML sanitization (Ganss.Xss HtmlSanitizer) used instead of Html.Raw / HtmlString for any
    /// value that is placed in ViewBag/ViewData and rendered by Razor views.
    /// Policy source order:
    ///   1. Azure Key Vault secret (KEYVAULT_URI + HTML_SANITIZER_POLICY_SECRET_NAME, default "html-sanitizer-policy"),
    ///      authenticated with DefaultAzureCredential (managed identity in Azure).
    ///   2. HTML_SANITIZER_POLICY_JSON environment variable.
    ///   3. Restrictive built-in default.
    /// </summary>
    public static class HtmlSanitization
    {
        public const string KeyVaultUriVariable = "KEYVAULT_URI";
        public const string PolicySecretNameVariable = "HTML_SANITIZER_POLICY_SECRET_NAME";
        public const string PolicyJsonVariable = "HTML_SANITIZER_POLICY_JSON";
        private const string DefaultSecretName = "html-sanitizer-policy";

        private static readonly Lazy<HtmlSanitizer> SanitizerInstance =
            new Lazy<HtmlSanitizer>(CreateSanitizer, true);

        private static HtmlSanitizationPolicy LoadPolicy()
        {
            string json = null;

            var vaultUri = Environment.GetEnvironmentVariable(KeyVaultUriVariable);
            if (!string.IsNullOrWhiteSpace(vaultUri))
            {
                try
                {
                    var secretName = Environment.GetEnvironmentVariable(PolicySecretNameVariable);
                    if (string.IsNullOrWhiteSpace(secretName))
                    {
                        secretName = DefaultSecretName;
                    }
                    var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
                    json = client.GetSecret(secretName).Value.Value;
                }
                catch (Exception)
                {
                    // Key Vault unavailable: fall back to environment / default policy (fail closed).
                    json = null;
                }
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                json = Environment.GetEnvironmentVariable(PolicyJsonVariable);
            }

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var policy = JsonConvert.DeserializeObject<HtmlSanitizationPolicy>(json);
                    if (policy != null)
                    {
                        return policy;
                    }
                }
                catch (JsonException)
                {
                    // Malformed policy: use the restrictive default.
                }
            }

            return HtmlSanitizationPolicy.Default();
        }

        private static HtmlSanitizer CreateSanitizer()
        {
            var policy = LoadPolicy();
            var defaults = HtmlSanitizationPolicy.Default();
            var sanitizer = new HtmlSanitizer();

            sanitizer.AllowedTags.Clear();
            foreach (var tag in policy.AllowedTags ?? defaults.AllowedTags)
            {
                sanitizer.AllowedTags.Add(tag);
            }

            sanitizer.AllowedAttributes.Clear();
            foreach (var attr in policy.AllowedAttributes ?? defaults.AllowedAttributes)
            {
                sanitizer.AllowedAttributes.Add(attr);
            }

            sanitizer.AllowedSchemes.Clear();
            foreach (var scheme in policy.AllowedSchemes ?? defaults.AllowedSchemes)
            {
                sanitizer.AllowedSchemes.Add(scheme);
            }

            sanitizer.AllowedCssProperties.Clear();
            foreach (var css in policy.AllowedCssProperties ?? defaults.AllowedCssProperties)
            {
                sanitizer.AllowedCssProperties.Add(css);
            }

            return sanitizer;
        }

        /// <summary>Sanitizes an HTML/text fragment according to the configured policy.</summary>
        public static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            return SanitizerInstance.Value.Sanitize(value);
        }

        /// <summary>
        /// Sanitizes a value destined for ViewBag. Strings are sanitized; numeric/boolean values are
        /// returned unchanged (they cannot carry markup); anything else is sanitized via its string form.
        /// </summary>
        public static object SanitizeValue(object value)
        {
            if (value == null)
            {
                return null;
            }
            var text = value as string;
            if (text != null)
            {
                return Sanitize(text);
            }
            if (value is int || value is long || value is short || value is bool || value is decimal || value is double)
            {
                return value;
            }
            return Sanitize(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>Returns a copy of the SelectList whose display text has been sanitized.</summary>
        public static SelectList SanitizeSelectList(SelectList list)
        {
            if (list == null)
            {
                return null;
            }
            var items = list.Select(i => new { i.Value, Text = Sanitize(i.Text) }).ToList();
            return new SelectList(items, "Value", "Text", list.SelectedValue);
        }
    }
}
