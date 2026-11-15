using System;
using System.Collections.Generic;
using System.Linq;
using Ganss.Xss;
using Google.Cloud.SecretManager.V1;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Sanitization policy (allowed tags / attributes / URI schemes) that is stored as a versioned
    /// secret in GCP Secret Manager. Example secret payload:
    /// { "AllowedTags": ["b","i","em","strong","p"], "AllowedAttributes": ["class"], "AllowedSchemes": ["https"] }
    /// </summary>
    public class HtmlSanitizerPolicy
    {
        public List<string> AllowedTags { get; set; } = new List<string>();
        public List<string> AllowedAttributes { get; set; } = new List<string>();
        public List<string> AllowedSchemes { get; set; } = new List<string> { "https" };
    }

    /// <summary>
    /// Centralised replacement for @Html.Raw()/HtmlString: every value that is rendered from
    /// ViewBag / ViewData goes through Ganss.Xss.HtmlSanitizer first.
    /// The policy secret is resolved from the SANITIZER_POLICY_SECRET environment variable, which holds
    /// the secret version resource name, e.g. "projects/my-project/secrets/html-sanitizer-policy/versions/latest".
    /// When it is not configured or unavailable, a strict policy (no tags, no attributes) is used.
    /// </summary>
    public static class HtmlSanitizerProvider
    {
        public const string PolicySecretEnvVar = "SANITIZER_POLICY_SECRET";

        private static readonly Lazy<HtmlSanitizer> Sanitizer = new Lazy<HtmlSanitizer>(BuildSanitizer);

        public static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return Sanitizer.Value.Sanitize(input);
        }

        private static HtmlSanitizer BuildSanitizer()
        {
            var policy = LoadPolicy();
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedSchemes.Clear();
            foreach (var t in policy.AllowedTags) sanitizer.AllowedTags.Add(t);
            foreach (var a in policy.AllowedAttributes) sanitizer.AllowedAttributes.Add(a);
            foreach (var s in policy.AllowedSchemes) sanitizer.AllowedSchemes.Add(s);
            return sanitizer;
        }

        private static HtmlSanitizerPolicy LoadPolicy()
        {
            var secretName = Environment.GetEnvironmentVariable(PolicySecretEnvVar);
            if (string.IsNullOrWhiteSpace(secretName))
            {
                return new HtmlSanitizerPolicy();
            }
            try
            {
                var client = SecretManagerServiceClient.Create();
                var response = client.AccessSecretVersion(secretName);
                var json = response.Payload.Data.ToStringUtf8();
                return JsonConvert.DeserializeObject<HtmlSanitizerPolicy>(json) ?? new HtmlSanitizerPolicy();
            }
            catch (Exception ex)
            {
                // Fail closed: strict policy when the secret cannot be read.
                Console.Error.WriteLine("{\"severity\":\"WARNING\",\"message\":\"Unable to load HTML sanitizer policy from Secret Manager; using strict policy\",\"error\":\"" + ex.GetType().Name + "\"}");
                return new HtmlSanitizerPolicy();
            }
        }
    }
}
