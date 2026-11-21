using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Ganss.Xss;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Central HTML sanitization (Ganss.Xss HtmlSanitizer). The allow-list policy is loaded from
    /// Azure Key Vault (JSON secret) using DefaultAzureCredential (Managed Identity in Azure).
    /// Configuration (environment variables or appSettings):
    ///   KEY_VAULT_URI                 - e.g. https://my-vault.vault.azure.net/
    ///   HTML_SANITIZER_POLICY_SECRET  - secret name (default: HtmlSanitizerPolicy)
    /// If Key Vault is not configured/reachable the strictest policy (no markup allowed) is used.
    /// </summary>
    public static class HtmlSanitizerProvider
    {
        private const string DefaultSecretName = "HtmlSanitizerPolicy";
        private static readonly Lazy<HtmlSanitizer> SanitizerInstance = new Lazy<HtmlSanitizer>(Build);

        private static string Setting(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? ConfigurationManager.AppSettings[name] : value;
        }

        private static HtmlSanitizationPolicy LoadPolicy()
        {
            var vaultUri = Setting("KEY_VAULT_URI");
            if (string.IsNullOrWhiteSpace(vaultUri))
            {
                return new HtmlSanitizationPolicy();
            }

            try
            {
                var secretName = Setting("HTML_SANITIZER_POLICY_SECRET");
                if (string.IsNullOrWhiteSpace(secretName)) secretName = DefaultSecretName;

                var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
                var json = client.GetSecret(secretName).Value.Value;

                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(HtmlSanitizationPolicy));
                    return (HtmlSanitizationPolicy)serializer.ReadObject(ms) ?? new HtmlSanitizationPolicy();
                }
            }
            catch (Exception)
            {
                // Fail closed: no markup allowed when the policy cannot be retrieved.
                return new HtmlSanitizationPolicy();
            }
        }

        private static HtmlSanitizer Build()
        {
            var policy = LoadPolicy();
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedSchemes.Clear();
            foreach (var t in policy.AllowedTags ?? new System.Collections.Generic.List<string>()) sanitizer.AllowedTags.Add(t);
            foreach (var a in policy.AllowedAttributes ?? new System.Collections.Generic.List<string>()) sanitizer.AllowedAttributes.Add(a);
            foreach (var s in policy.AllowedSchemes ?? new System.Collections.Generic.List<string>()) sanitizer.AllowedSchemes.Add(s);
            return sanitizer;
        }

        /// <summary>Sanitizes an HTML fragment according to the Key Vault policy.</summary>
        public static string Sanitize(string html)
        {
            return string.IsNullOrEmpty(html) ? html : SanitizerInstance.Value.Sanitize(html);
        }

        /// <summary>
        /// Sanitizes a value that will be rendered through Razor's automatic HTML encoding
        /// (e.g. ViewBag values). Markup is sanitized and then decoded so Razor does not double-encode.
        /// </summary>
        public static string SanitizeText(string value)
        {
            return string.IsNullOrEmpty(value) ? value : WebUtility.HtmlDecode(Sanitize(value));
        }
    }
}
