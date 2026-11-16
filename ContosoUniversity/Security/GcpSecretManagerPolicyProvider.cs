using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Loads the HTML sanitization policy from a versioned GCP Secret Manager secret using the
    /// Secret Manager REST API and the workload's service account (metadata server token).
    /// Configuration (environment variables):
    ///   GCP_PROJECT_ID                     - GCP project id (required to use Secret Manager)
    ///   HTML_SANITIZER_POLICY_SECRET       - secret name (default: html-sanitizer-policy)
    ///   HTML_SANITIZER_POLICY_VERSION      - secret version (default: latest)
    /// </summary>
    public static class GcpSecretManagerPolicyProvider
    {
        private const string MetadataTokenUrl =
            "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/default/token";

        public static SanitizerPolicy Load()
        {
            var projectId = Environment.GetEnvironmentVariable("GCP_PROJECT_ID");
            if (string.IsNullOrWhiteSpace(projectId))
            {
                return SanitizerPolicy.Default();
            }

            var secret = Environment.GetEnvironmentVariable("HTML_SANITIZER_POLICY_SECRET");
            if (string.IsNullOrWhiteSpace(secret)) secret = "html-sanitizer-policy";
            var version = Environment.GetEnvironmentVariable("HTML_SANITIZER_POLICY_VERSION");
            if (string.IsNullOrWhiteSpace(version)) version = "latest";

            try
            {
                // Run on thread-pool thread to avoid ASP.NET sync-context deadlocks.
                var json = Task.Run(() => FetchSecretAsync(projectId, secret, version)).GetAwaiter().GetResult();
                var policy = JsonConvert.DeserializeObject<SanitizerPolicy>(json);
                return policy ?? SanitizerPolicy.Default();
            }
            catch (Exception)
            {
                // Fail closed with a restrictive default policy.
                return SanitizerPolicy.Default();
            }
        }

        /// <summary>
        /// Reads a secret payload from GCP Secret Manager (shared by policy loading and cloud initialization).
        /// </summary>
        public static string AccessSecret(string projectId, string secret, string version)
        {
            return Task.Run(() => FetchSecretAsync(projectId, secret, version)).GetAwaiter().GetResult();
        }

        private static async Task<string> FetchSecretAsync(string projectId, string secret, string version)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
            {
                var tokenRequest = new HttpRequestMessage(HttpMethod.Get, MetadataTokenUrl);
                tokenRequest.Headers.Add("Metadata-Flavor", "Google");
                var tokenResponse = await client.SendAsync(tokenRequest).ConfigureAwait(false);
                tokenResponse.EnsureSuccessStatusCode();
                var tokenJson = JObject.Parse(await tokenResponse.Content.ReadAsStringAsync().ConfigureAwait(false));
                var accessToken = (string)tokenJson["access_token"];

                var url = string.Format(
                    "https://secretmanager.googleapis.com/v1/projects/{0}/secrets/{1}/versions/{2}:access",
                    Uri.EscapeDataString(projectId), Uri.EscapeDataString(secret), Uri.EscapeDataString(version));
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var body = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                var data = (string)body["payload"]["data"];
                return Encoding.UTF8.GetString(Convert.FromBase64String(data));
            }
        }
    }
}
