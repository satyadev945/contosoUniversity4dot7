using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Ganss.Xss;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Ganss.Xss HtmlSanitizer wrapper. The allowed tags / attributes / schemes / CSS properties
    /// are stored centrally in AWS Secrets Manager (secret id from HTML_SANITIZER_SECRET_ID,
    /// region from AWS_REGION / AWS_DEFAULT_REGION) as JSON:
    /// {"allowedTags":["b","i"],"allowedAttributes":["class"],"allowedSchemes":["https"],"allowedCssProperties":["color"]}
    /// If the secret is not configured or unavailable, a strict default (no HTML allowed) is used.
    /// </summary>
    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        private static readonly Lazy<Task<HtmlSanitizer>> LazySanitizer =
            new Lazy<Task<HtmlSanitizer>>(CreateSanitizerAsync);

        private class SanitizerConfig
        {
            [JsonProperty("allowedTags")]
            public string[] AllowedTags { get; set; }
            [JsonProperty("allowedAttributes")]
            public string[] AllowedAttributes { get; set; }
            [JsonProperty("allowedSchemes")]
            public string[] AllowedSchemes { get; set; }
            [JsonProperty("allowedCssProperties")]
            public string[] AllowedCssProperties { get; set; }
        }

        private static async Task<SanitizerConfig> LoadConfigAsync()
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
                    var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId })
                        .ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<SanitizerConfig>(response.SecretString);
                }
            }
            catch (Exception)
            {
                return null; // fall back to strict defaults
            }
        }

        private static async Task<HtmlSanitizer> CreateSanitizerAsync()
        {
            var config = await LoadConfigAsync().ConfigureAwait(false);
            var sanitizer = new HtmlSanitizer();

            // Strict by default: nothing allowed unless the centrally managed config permits it.
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedCssProperties.Clear();
            sanitizer.AllowedSchemes.Clear();

            if (config != null)
            {
                foreach (var t in config.AllowedTags ?? new string[0]) sanitizer.AllowedTags.Add(t);
                foreach (var a in config.AllowedAttributes ?? new string[0]) sanitizer.AllowedAttributes.Add(a);
                foreach (var c in config.AllowedCssProperties ?? new string[0]) sanitizer.AllowedCssProperties.Add(c);
                foreach (var s in config.AllowedSchemes ?? new string[0]) sanitizer.AllowedSchemes.Add(s);
            }
            return sanitizer;
        }

        public async Task<string> SanitizeAsync(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }
            var sanitizer = await LazySanitizer.Value.ConfigureAwait(false);
            lock (sanitizer)
            {
                return sanitizer.Sanitize(html);
            }
        }

        public async Task<T> SanitizeModelAsync<T>(T model)
        {
            if (model == null)
            {
                return model;
            }
            var collection = model as IEnumerable;
            if (collection != null && !(model is string))
            {
                foreach (var item in collection)
                {
                    await SanitizeObjectAsync(item).ConfigureAwait(false);
                }
            }
            else
            {
                await SanitizeObjectAsync(model).ConfigureAwait(false);
            }
            return model;
        }

        private async Task SanitizeObjectAsync(object item)
        {
            if (item == null)
            {
                return;
            }
            var props = item.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite
                            && p.GetIndexParameters().Length == 0);
            foreach (var p in props)
            {
                var value = (string)p.GetValue(item);
                var clean = await SanitizeAsync(value).ConfigureAwait(false);
                if (!string.Equals(value, clean, StringComparison.Ordinal))
                {
                    p.SetValue(item, clean);
                }
            }
        }
    }
}
