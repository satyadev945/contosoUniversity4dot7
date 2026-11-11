using System;
using System.Threading;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Ganss.Xss;
using Newtonsoft.Json;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Sanitizes user supplied HTML using Ganss.Xss.HtmlSanitizer. The allow-list of tags/attributes
    /// is loaded from AWS Secrets Manager (env: HTML_SANITIZER_SECRET_ID, AWS_REGION).
    /// </summary>
    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        private static readonly Lazy<IHtmlSanitizerService> _instance =
            new Lazy<IHtmlSanitizerService>(() => new HtmlSanitizerService(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>Process-wide instance (the MVC controllers are created without DI).</summary>
        public static IHtmlSanitizerService Instance
        {
            get { return _instance.Value; }
        }

        private readonly HtmlSanitizer _sanitizer;

        public HtmlSanitizerService()
            : this(LoadSettings())
        {
        }

        public HtmlSanitizerService(HtmlSanitizerSettings settings)
        {
            settings = settings ?? HtmlSanitizerSettings.Default();
            _sanitizer = new HtmlSanitizer();
            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedAttributes.Clear();
            _sanitizer.AllowedSchemes.Clear();
            _sanitizer.AllowedCssProperties.Clear();
            foreach (var t in settings.AllowedTags) _sanitizer.AllowedTags.Add(t);
            foreach (var a in settings.AllowedAttributes) _sanitizer.AllowedAttributes.Add(a);
            foreach (var s in settings.AllowedSchemes) _sanitizer.AllowedSchemes.Add(s);
            foreach (var c in settings.AllowedCssProperties) _sanitizer.AllowedCssProperties.Add(c);
        }

        public string Sanitize(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }
            return _sanitizer.Sanitize(html);
        }

        private static HtmlSanitizerSettings LoadSettings()
        {
            var secretId = Environment.GetEnvironmentVariable("HTML_SANITIZER_SECRET_ID");
            if (string.IsNullOrWhiteSpace(secretId))
            {
                return HtmlSanitizerSettings.Default();
            }

            try
            {
                var region = Environment.GetEnvironmentVariable("AWS_REGION")
                             ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
                using (var client = string.IsNullOrWhiteSpace(region)
                    ? new AmazonSecretsManagerClient()
                    : new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region)))
                {
                    var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId })
                        .GetAwaiter().GetResult();
                    var settings = JsonConvert.DeserializeObject<HtmlSanitizerSettings>(response.SecretString);
                    return settings ?? HtmlSanitizerSettings.Default();
                }
            }
            catch (Exception ex)
            {
                // Fail closed: fall back to the strictest (plain text only) policy.
                Console.Error.WriteLine("{\"level\":\"error\",\"message\":\"Unable to load HTML sanitizer settings from AWS Secrets Manager\",\"error\":\"" +
                                        ex.Message.Replace("\"", "'") + "\"}");
                return HtmlSanitizerSettings.Default();
            }
        }
    }
}
