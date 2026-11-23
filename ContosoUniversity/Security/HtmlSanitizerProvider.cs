using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Ganss.Xss;
using Newtonsoft.Json;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Central entry point for sanitizing any value that flows into a Razor view (ViewBag, titles, etc.).
    /// The sanitizer allow-list is loaded once from AWS Secrets Manager (HTML_SANITIZER_SECRET_ID,
    /// region from AWS_REGION / AWS_DEFAULT_REGION). When no secret is configured, or it cannot be
    /// read, a restrictive built-in allow-list is used (fail-safe).
    /// </summary>
    public static class HtmlSanitizerProvider
    {
        private const string SecretIdVariable = "HTML_SANITIZER_SECRET_ID";

        private static readonly Lazy<HtmlSanitizer> SanitizerInstance =
            new Lazy<HtmlSanitizer>(CreateSanitizer, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>Sanitizes HTML, removing disallowed tags/attributes/schemes.</summary>
        public static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return SanitizerInstance.Value.Sanitize(input);
        }

        /// <summary>
        /// Sanitizes a value that will be rendered as plain text (Razor still HTML-encodes on output),
        /// avoiding double encoding of entities produced by the sanitizer serializer.
        /// </summary>
        public static string SanitizeText(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return WebUtility.HtmlDecode(Sanitize(input));
        }

        /// <summary>Returns a copy of the SelectList whose item texts have been sanitized.</summary>
        public static SelectList SanitizeSelectList(SelectList source)
        {
            if (source == null)
            {
                return null;
            }
            var items = source.Select(i => new SelectListItem
            {
                Value = i.Value,
                Text = SanitizeText(i.Text),
                Selected = i.Selected
            }).ToList();
            return new SelectList(items, "Value", "Text", source.SelectedValue);
        }

        private static HtmlSanitizer CreateSanitizer()
        {
            var config = LoadConfig() ?? HtmlSanitizerConfig.Default();
            var defaults = HtmlSanitizerConfig.Default();

            var sanitizer = new HtmlSanitizer();
            Replace(sanitizer.AllowedTags, config.AllowedTags ?? defaults.AllowedTags);
            Replace(sanitizer.AllowedAttributes, config.AllowedAttributes ?? defaults.AllowedAttributes);
            Replace(sanitizer.AllowedSchemes, config.AllowedSchemes ?? defaults.AllowedSchemes);
            Replace(sanitizer.AllowedCssProperties, config.AllowedCssProperties ?? defaults.AllowedCssProperties);
            return sanitizer;
        }

        private static void Replace(ISet<string> target, IEnumerable<string> values)
        {
            target.Clear();
            foreach (var v in values.Where(v => !string.IsNullOrWhiteSpace(v)))
            {
                target.Add(v.Trim());
            }
        }

        private static HtmlSanitizerConfig LoadConfig()
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

                // Credentials are resolved via the default AWS chain (IAM role for ECS/EKS/EC2, env vars, etc.)
                using (var client = string.IsNullOrWhiteSpace(region)
                    ? new AmazonSecretsManagerClient()
                    : new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region)))
                {
                    var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId })
                        .GetAwaiter().GetResult();
                    if (string.IsNullOrWhiteSpace(response.SecretString))
                    {
                        return null;
                    }
                    return JsonConvert.DeserializeObject<HtmlSanitizerConfig>(response.SecretString);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("{\"level\":\"warn\",\"message\":\"Unable to load HTML sanitizer configuration from AWS Secrets Manager; using defaults\",\"error\":\""
                    + ex.GetType().Name + "\"}");
                return null;
            }
        }
    }
}
