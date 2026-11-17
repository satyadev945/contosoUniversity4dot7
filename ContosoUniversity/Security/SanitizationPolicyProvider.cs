using System;
using System.Text.Json;
using Google.Cloud.SecretManager.V1;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Loads the HTML sanitization policy from GCP Secret Manager.
    /// Configuration (environment variables):
    ///   GCP_PROJECT_ID                  - GCP project hosting the secret
    ///   SANITIZER_POLICY_SECRET_NAME    - secret id (default: html-sanitizer-policy)
    ///   SANITIZER_POLICY_SECRET_VERSION - secret version (default: latest)
    /// Falls back to the strict default policy if the secret is not configured/available.
    /// </summary>
    public static class SanitizationPolicyProvider
    {
        public static SanitizationPolicy Load()
        {
            var projectId = Environment.GetEnvironmentVariable("GCP_PROJECT_ID");
            if (string.IsNullOrWhiteSpace(projectId))
            {
                return new SanitizationPolicy();
            }

            var secretName = Environment.GetEnvironmentVariable("SANITIZER_POLICY_SECRET_NAME");
            if (string.IsNullOrWhiteSpace(secretName)) secretName = "html-sanitizer-policy";
            var version = Environment.GetEnvironmentVariable("SANITIZER_POLICY_SECRET_VERSION");
            if (string.IsNullOrWhiteSpace(version)) version = "latest";

            try
            {
                var client = SecretManagerServiceClient.Create();
                var versionName = new SecretVersionName(projectId, secretName, version);
                var response = client.AccessSecretVersion(versionName);
                var json = response.Payload.Data.ToStringUtf8();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<SanitizationPolicy>(json, options) ?? new SanitizationPolicy();
            }
            catch (Exception)
            {
                // Fail closed: strictest policy (all markup stripped)
                return new SanitizationPolicy();
            }
        }
    }
}
