using System;
using System.Text.Json;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Loads the sanitiser allow-list from AWS Secrets Manager. The secret id comes from HTML_SANITIZER_SECRET_ID
    /// and the region from AWS_REGION / AWS_DEFAULT_REGION (credentials via the default AWS credential chain,
    /// e.g. IAM role for service accounts / task role). Falls back to the safe defaults when no secret is configured
    /// or it cannot be read.
    /// </summary>
    public static class HtmlSanitizerOptionsProvider
    {
        public const string SecretIdEnvVar = "HTML_SANITIZER_SECRET_ID";

        public static HtmlSanitizerOptions Load()
        {
            var secretId = Environment.GetEnvironmentVariable(SecretIdEnvVar);
            if (string.IsNullOrWhiteSpace(secretId))
            {
                return new HtmlSanitizerOptions();
            }

            try
            {
                var regionName = Environment.GetEnvironmentVariable("AWS_REGION")
                                 ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
                using (var client = string.IsNullOrWhiteSpace(regionName)
                           ? new AmazonSecretsManagerClient()
                           : new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(regionName)))
                {
                    var response = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId })
                                         .GetAwaiter().GetResult();
                    var options = JsonSerializer.Deserialize<HtmlSanitizerOptions>(
                        response.SecretString,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return options ?? new HtmlSanitizerOptions();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("{\"level\":\"warn\",\"message\":\"Unable to load HtmlSanitizer configuration from AWS Secrets Manager; using defaults\",\"error\":\""
                                        + ex.GetType().Name + "\"}");
                return new HtmlSanitizerOptions();
            }
        }
    }
}
