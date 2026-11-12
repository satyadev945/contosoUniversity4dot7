using System;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Ganss.Xss;
using Newtonsoft.Json;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Central entry point for sanitizing any value that is handed to a Razor view (replaces Html.Raw /
    /// HtmlString usage). The allowed tag/attribute configuration is read from AWS Secrets Manager.
    ///
    /// Environment variables:
    ///   HTML_SANITIZER_SECRET_ID  - Secrets Manager secret id/ARN holding a JSON <see cref="SanitizerConfig"/>
    ///   AWS_REGION                - region of the secret (standard AWS SDK variable)
    /// Credentials come from the default AWS credential chain (IAM role for ECS/EKS/EC2/Lambda).
    /// If the secret is not configured or unavailable the strictest policy is used (no tags, no attributes).
    /// </summary>
    public static class HtmlSanitizerProvider
    {
        private static readonly Lazy<HtmlSanitizer> _sanitizer = new Lazy<HtmlSanitizer>(CreateSanitizer, true);

        /// <summary>Sanitizes untrusted HTML, keeping only the tags/attributes allowed by the central config.</summary>
        public static string SanitizeHtml(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return _sanitizer.Value.Sanitize(input);
        }

        /// <summary>
        /// Sanitizes a value that will be rendered as text (and therefore HTML-encoded again by Razor).
        /// Dangerous markup is removed and the result is decoded so it is not double-encoded on output.
        /// </summary>
        public static string SanitizeText(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return WebUtility.HtmlDecode(_sanitizer.Value.Sanitize(input));
        }

        private static HtmlSanitizer CreateSanitizer()
        {
            var config = LoadConfig() ?? new SanitizerConfig();
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedSchemes.Clear();
            sanitizer.AllowedCssProperties.Clear();

            foreach (var tag in config.AllowedTags ?? new System.Collections.Generic.List<string>())
            {
                sanitizer.AllowedTags.Add(tag);
            }
            foreach (var attr in config.AllowedAttributes ?? new System.Collections.Generic.List<string>())
            {
                sanitizer.AllowedAttributes.Add(attr);
            }
            foreach (var scheme in config.AllowedSchemes ?? new System.Collections.Generic.List<string>())
            {
                sanitizer.AllowedSchemes.Add(scheme);
            }
            return sanitizer;
        }

        private static SanitizerConfig LoadConfig()
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

                using (var client = string.IsNullOrWhiteSpace(region)
                    ? new AmazonSecretsManagerClient()
                    : new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region)))
                {
                    var response = Task.Run(() => client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId }))
                                       .GetAwaiter().GetResult();
                    return JsonConvert.DeserializeObject<SanitizerConfig>(response.SecretString);
                }
            }
            catch (Exception ex)
            {
                // Fail closed: fall back to the strictest policy (plain text only).
                Console.Error.WriteLine("{\"level\":\"warn\",\"message\":\"Unable to load HTML sanitizer config from AWS Secrets Manager; using strict defaults\",\"error\":\""
                                        + ex.GetType().Name + "\"}");
                return null;
            }
        }
    }
}
