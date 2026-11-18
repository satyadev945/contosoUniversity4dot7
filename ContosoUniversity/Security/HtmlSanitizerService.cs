using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Ganss.Xss;
using Newtonsoft.Json;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Ganss.Xss.HtmlSanitizer wrapper. The allowed tags / attributes / schemes / CSS properties are
    /// loaded from AWS Secrets Manager. The secret id is read from the HTML_SANITIZER_SECRET_ID
    /// environment variable (fallback: "HtmlSanitizer:SecretId" app setting) and the region from
    /// AWS_REGION / AWS_DEFAULT_REGION. Credentials are resolved by the default AWS credential chain
    /// (IAM role for service accounts / ECS task role / instance profile).
    /// If the secret is unavailable the most restrictive configuration is used (all markup stripped).
    /// </summary>
    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        private static readonly Lazy<HtmlSanitizerService> LazyInstance =
            new Lazy<HtmlSanitizerService>(() => new HtmlSanitizerService());

        private readonly HtmlSanitizer sanitizer;

        public static HtmlSanitizerService Instance
        {
            get { return LazyInstance.Value; }
        }

        public HtmlSanitizerService()
            : this(LoadConfig())
        {
        }

        public HtmlSanitizerService(HtmlSanitizerConfig config)
        {
            config = config ?? HtmlSanitizerConfig.Default();

            sanitizer = new HtmlSanitizer();
            Replace(sanitizer.AllowedTags, config.AllowedTags);
            Replace(sanitizer.AllowedAttributes, config.AllowedAttributes);
            Replace(sanitizer.AllowedSchemes, config.AllowedSchemes);
            Replace(sanitizer.AllowedCssProperties, config.AllowedCssProperties);
        }

        public string Sanitize(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }
            return sanitizer.Sanitize(html);
        }

        private static void Replace(ISet<string> target, IEnumerable<string> values)
        {
            target.Clear();
            if (values == null)
            {
                return;
            }
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    target.Add(value.Trim());
                }
            }
        }

        private static string GetSecretId()
        {
            var secretId = Environment.GetEnvironmentVariable("HTML_SANITIZER_SECRET_ID");
            if (string.IsNullOrWhiteSpace(secretId))
            {
                secretId = ConfigurationManager.AppSettings["HtmlSanitizer:SecretId"];
            }
            return secretId;
        }

        private static HtmlSanitizerConfig LoadConfig()
        {
            var secretId = GetSecretId();
            if (string.IsNullOrWhiteSpace(secretId))
            {
                return HtmlSanitizerConfig.Default();
            }

            try
            {
                var region = Environment.GetEnvironmentVariable("AWS_REGION")
                             ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");

                using (var client = string.IsNullOrWhiteSpace(region)
                    ? new AmazonSecretsManagerClient()
                    : new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region)))
                {
                    var response = client.GetSecretValue(new GetSecretValueRequest { SecretId = secretId });
                    var config = JsonConvert.DeserializeObject<HtmlSanitizerConfig>(response.SecretString);
                    return config ?? HtmlSanitizerConfig.Default();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Unable to load HTML sanitizer configuration from AWS Secrets Manager: " + ex.Message);
                return HtmlSanitizerConfig.Default();
            }
        }
    }
}
