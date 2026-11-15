using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using Ganss.XSS;
using Newtonsoft.Json;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Centralised HTML sanitization (Ganss.Xss HtmlSanitizer).
    /// The allow-list policy (tags / attributes / URI schemes) is a JSON secret that is stored in
    /// Azure Key Vault and surfaced to the app via configuration, e.g. in Azure App Service:
    ///   HTML_SANITIZER_POLICY = @Microsoft.KeyVault(SecretUri=https://&lt;vault&gt;.vault.azure.net/secrets/html-sanitizer-policy/)
    /// Policy JSON example:
    ///   { "allowedTags": ["b","i","em","strong"], "allowedAttributes": ["class"], "allowedSchemes": ["https"] }
    /// When no policy is configured the sanitizer fails closed (no tags/attributes allowed).
    /// </summary>
    public static class HtmlSanitizerProvider
    {
        public const string PolicySettingName = "HTML_SANITIZER_POLICY";

        private class SanitizerPolicy
        {
            [JsonProperty("allowedTags")]
            public List<string> AllowedTags { get; set; }

            [JsonProperty("allowedAttributes")]
            public List<string> AllowedAttributes { get; set; }

            [JsonProperty("allowedSchemes")]
            public List<string> AllowedSchemes { get; set; }
        }

        private static readonly Lazy<HtmlSanitizer> SanitizerInstance = new Lazy<HtmlSanitizer>(CreateSanitizer);

        private static HtmlSanitizer CreateSanitizer()
        {
            var sanitizer = new HtmlSanitizer();
            var json = Environment.GetEnvironmentVariable(PolicySettingName)
                       ?? ConfigurationManager.AppSettings[PolicySettingName];

            // Fail closed by default: nothing is allowed unless the Key Vault policy permits it.
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedSchemes.Clear();

            if (string.IsNullOrWhiteSpace(json))
            {
                return sanitizer;
            }

            try
            {
                var policy = JsonConvert.DeserializeObject<SanitizerPolicy>(json);
                if (policy == null)
                {
                    return sanitizer;
                }
                if (policy.AllowedTags != null)
                {
                    foreach (var tag in policy.AllowedTags) sanitizer.AllowedTags.Add(tag);
                }
                if (policy.AllowedAttributes != null)
                {
                    foreach (var attr in policy.AllowedAttributes) sanitizer.AllowedAttributes.Add(attr);
                }
                if (policy.AllowedSchemes != null)
                {
                    foreach (var scheme in policy.AllowedSchemes) sanitizer.AllowedSchemes.Add(scheme);
                }
            }
            catch (JsonException)
            {
                // Malformed policy: keep the fail-closed configuration.
            }

            return sanitizer;
        }

        /// <summary>Sanitizes an HTML fragment according to the Key Vault policy.</summary>
        public static string SanitizeHtml(string html)
        {
            return string.IsNullOrEmpty(html) ? html : SanitizerInstance.Value.Sanitize(html);
        }

        /// <summary>
        /// Sanitizes user-supplied text that is later written to a view through Razor (which HTML-encodes it).
        /// The sanitizer output is decoded once so that Razor's own encoding does not double-encode.
        /// </summary>
        public static string SanitizeText(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return WebUtility.HtmlDecode(SanitizerInstance.Value.Sanitize(input));
        }
    }
}
