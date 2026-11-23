using System;
using System.Collections.Generic;
using System.Configuration;
using Newtonsoft.Json;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Allowed tags/attributes policy for the HTML sanitizer.
    /// The policy is stored as a JSON secret in Azure Key Vault (secret name: HtmlSanitizerPolicy)
    /// and surfaced to the application through the "HtmlSanitizerPolicy" environment variable /
    /// app setting (e.g. an Azure App Service Key Vault reference:
    /// @Microsoft.KeyVault(SecretUri=https://&lt;vault&gt;.vault.azure.net/secrets/HtmlSanitizerPolicy/)).
    /// Example JSON: {"AllowedTags":["b","i","em","strong","p","br"],"AllowedAttributes":["class"],"AllowedSchemes":["https"]}
    /// </summary>
    public class HtmlSanitizerPolicy
    {
        public const string SettingName = "HtmlSanitizerPolicy";

        public List<string> AllowedTags { get; set; }
        public List<string> AllowedAttributes { get; set; }
        public List<string> AllowedSchemes { get; set; }

        public static HtmlSanitizerPolicy Default()
        {
            return new HtmlSanitizerPolicy
            {
                AllowedTags = new List<string> { "b", "i", "em", "strong", "p", "br", "ul", "ol", "li" },
                AllowedAttributes = new List<string> { "class" },
                AllowedSchemes = new List<string> { "https" }
            };
        }

        /// <summary>
        /// Loads the policy from the environment variable (Key Vault backed) first, then app settings,
        /// falling back to a restrictive default policy.
        /// </summary>
        public static HtmlSanitizerPolicy Load()
        {
            string json = Environment.GetEnvironmentVariable(SettingName);
            if (string.IsNullOrWhiteSpace(json))
            {
                json = ConfigurationManager.AppSettings[SettingName];
            }
            if (string.IsNullOrWhiteSpace(json))
            {
                return Default();
            }
            try
            {
                var policy = JsonConvert.DeserializeObject<HtmlSanitizerPolicy>(json);
                var def = Default();
                if (policy == null) return def;
                policy.AllowedTags = policy.AllowedTags ?? def.AllowedTags;
                policy.AllowedAttributes = policy.AllowedAttributes ?? def.AllowedAttributes;
                policy.AllowedSchemes = policy.AllowedSchemes ?? def.AllowedSchemes;
                return policy;
            }
            catch (JsonException)
            {
                return Default();
            }
        }
    }
}
