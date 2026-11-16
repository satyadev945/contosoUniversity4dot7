using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Ganss.Xss;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Allow-list configuration for the HTML sanitizer. Stored as JSON in AWS Secrets Manager
    /// (secret id supplied through the SANITIZER_CONFIG_SECRET_ID environment variable) so it can be
    /// managed centrally across tenants/environments.
    /// Example secret value:
    /// {"allowedTags":["b","i","em","strong","p","br"],"allowedAttributes":["class"],"allowedSchemes":["https"]}
    /// </summary>
    public class HtmlSanitizerConfig
    {
        [JsonProperty("allowedTags")]
        public List<string> AllowedTags { get; set; }

        [JsonProperty("allowedAttributes")]
        public List<string> AllowedAttributes { get; set; }

        [JsonProperty("allowedSchemes")]
        public List<string> AllowedSchemes { get; set; }

        /// <summary>Secure default: no HTML is allowed at all (everything is stripped).</summary>
        public static HtmlSanitizerConfig Default()
        {
            return new HtmlSanitizerConfig
            {
                AllowedTags = new List<string>(),
                AllowedAttributes = new List<string>(),
                AllowedSchemes = new List<string> { "https" }
            };
        }
    }

    /// <summary>
    /// Central Ganss.Xss HtmlSanitizer wrapper. Replaces any use of Html.Raw/HtmlString: values placed in
    /// ViewBag/SelectLists are sanitized here and then HTML-encoded by Razor on output.
    /// </summary>
    public static class HtmlSanitizerService
    {
        private const string SecretIdEnvVar = "SANITIZER_CONFIG_SECRET_ID";
        private const string RegionEnvVar = "AWS_REGION";

        private static readonly Lazy<HtmlSanitizerConfig> Config =
            new Lazy<HtmlSanitizerConfig>(LoadConfig, true);

        private static HtmlSanitizerConfig LoadConfig()
        {
            var secretId = Environment.GetEnvironmentVariable(SecretIdEnvVar);
            if (string.IsNullOrWhiteSpace(secretId))
            {
                return HtmlSanitizerConfig.Default();
            }

            try
            {
                var region = Environment.GetEnvironmentVariable(RegionEnvVar)
                             ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");

                using (var client = string.IsNullOrWhiteSpace(region)
                    ? new AmazonSecretsManagerClient()
                    : new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region)))
                {
                    var response = client
                        .GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId })
                        .GetAwaiter().GetResult();

                    var cfg = JsonConvert.DeserializeObject<HtmlSanitizerConfig>(response.SecretString);
                    if (cfg == null)
                    {
                        return HtmlSanitizerConfig.Default();
                    }
                    cfg.AllowedTags = cfg.AllowedTags ?? new List<string>();
                    cfg.AllowedAttributes = cfg.AllowedAttributes ?? new List<string>();
                    cfg.AllowedSchemes = cfg.AllowedSchemes ?? new List<string> { "https" };
                    return cfg;
                }
            }
            catch (Exception)
            {
                // Fail closed: if the secret cannot be read, fall back to the strictest policy.
                return HtmlSanitizerConfig.Default();
            }
        }

        private static HtmlSanitizer CreateSanitizer()
        {
            var cfg = Config.Value;
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedSchemes.Clear();
            foreach (var t in cfg.AllowedTags) sanitizer.AllowedTags.Add(t);
            foreach (var a in cfg.AllowedAttributes) sanitizer.AllowedAttributes.Add(a);
            foreach (var s in cfg.AllowedSchemes) sanitizer.AllowedSchemes.Add(s);
            return sanitizer;
        }

        /// <summary>Sanitizes a string; returns null for null input.</summary>
        public static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return CreateSanitizer().Sanitize(input);
        }

        /// <summary>
        /// Builds a SelectList whose display text has been sanitized.
        /// </summary>
        public static SelectList SafeSelectList<T>(
            IEnumerable<T> items,
            Func<T, object> valueSelector,
            Func<T, string> textSelector,
            object selectedValue = null)
        {
            var sanitizer = CreateSanitizer();
            var safeItems = items.Select(i => new SelectListItem
            {
                Value = Convert.ToString(valueSelector(i)),
                Text = sanitizer.Sanitize(textSelector(i) ?? string.Empty)
            }).ToList();

            return new SelectList(safeItems, "Value", "Text",
                selectedValue == null ? null : Convert.ToString(selectedValue));
        }
    }
}
