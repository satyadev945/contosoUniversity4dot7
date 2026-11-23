using System;
using System.Diagnostics;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Newtonsoft.Json;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Loads the HTML sanitization policy from Azure Key Vault.
    /// Configuration (environment variables, 12-factor):
    ///   KEY_VAULT_URI                       - e.g. https://my-vault.vault.azure.net/
    ///   HTML_SANITIZER_POLICY_SECRET_NAME   - secret name (default: HtmlSanitizerPolicy)
    ///   HTML_SANITIZER_POLICY_JSON          - optional; app setting populated through a
    ///                                         Key Vault reference (@Microsoft.KeyVault(...))
    /// Authentication uses DefaultAzureCredential (managed identity in Azure).
    /// If the policy cannot be loaded the strict (plain text only) policy is used.
    /// </summary>
    public static class HtmlSanitizationPolicyProvider
    {
        public const string DefaultSecretName = "HtmlSanitizerPolicy";

        public static HtmlSanitizationPolicy Load()
        {
            try
            {
                string json = Environment.GetEnvironmentVariable("HTML_SANITIZER_POLICY_JSON");

                if (string.IsNullOrWhiteSpace(json))
                {
                    string vaultUri = Environment.GetEnvironmentVariable("KEY_VAULT_URI");
                    if (!string.IsNullOrWhiteSpace(vaultUri))
                    {
                        string secretName = Environment.GetEnvironmentVariable("HTML_SANITIZER_POLICY_SECRET_NAME");
                        if (string.IsNullOrWhiteSpace(secretName))
                        {
                            secretName = DefaultSecretName;
                        }

                        var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
                        json = client.GetSecret(secretName).Value.Value;
                    }
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    Trace.TraceWarning("HTML sanitization policy not configured; using strict plain-text policy.");
                    return HtmlSanitizationPolicy.StrictDefault();
                }

                var policy = JsonConvert.DeserializeObject<HtmlSanitizationPolicy>(json);
                return policy ?? HtmlSanitizationPolicy.StrictDefault();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to load HTML sanitization policy; using strict plain-text policy. " + ex.Message);
                return HtmlSanitizationPolicy.StrictDefault();
            }
        }
    }
}
