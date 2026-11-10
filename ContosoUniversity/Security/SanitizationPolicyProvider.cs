using System;
using System.Configuration;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Newtonsoft.Json;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Loads the HTML sanitization policy (JSON) from Azure Key Vault.
    /// Configuration (environment variables take precedence over appSettings):
    ///   KEY_VAULT_URI                    e.g. https://my-vault.vault.azure.net/
    ///   HTML_SANITIZER_POLICY_SECRET     secret name (default: HtmlSanitizationPolicy)
    /// Authentication uses DefaultAzureCredential (managed identity in Azure).
    /// If Key Vault is not configured/unreachable, a strict policy (all markup stripped) is used.
    /// </summary>
    public static class SanitizationPolicyProvider
    {
        private const string DefaultSecretName = "HtmlSanitizationPolicy";

        public static SanitizationPolicy Load()
        {
            var vaultUri = GetSetting("KEY_VAULT_URI");
            if (string.IsNullOrWhiteSpace(vaultUri))
            {
                return new SanitizationPolicy();
            }

            try
            {
                var secretName = GetSetting("HTML_SANITIZER_POLICY_SECRET");
                if (string.IsNullOrWhiteSpace(secretName))
                {
                    secretName = DefaultSecretName;
                }

                var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
                var secret = client.GetSecret(secretName);
                var policy = JsonConvert.DeserializeObject<SanitizationPolicy>(secret.Value.Value);
                return policy ?? new SanitizationPolicy();
            }
            catch (Exception ex)
            {
                // Fail closed: strip all markup if the policy cannot be retrieved.
                System.Diagnostics.Trace.TraceError("Unable to load HTML sanitization policy from Key Vault: " + ex.Message);
                return new SanitizationPolicy();
            }
        }

        private static string GetSetting(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? ConfigurationManager.AppSettings[name] : value;
        }
    }
}
