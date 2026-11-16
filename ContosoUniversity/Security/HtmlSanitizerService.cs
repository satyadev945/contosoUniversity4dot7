using System;
using Ganss.XSS;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Ganss.XSS HtmlSanitizer-backed implementation. Policy (allowed tags, attributes, schemes)
    /// is loaded once from GCP Secret Manager and applied to a shared sanitizer instance.
    /// </summary>
    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        private static readonly Lazy<HtmlSanitizer> SharedSanitizer = new Lazy<HtmlSanitizer>(CreateSanitizer);

        public static IHtmlSanitizerService Instance { get; } = new HtmlSanitizerService();

        public string Sanitize(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }
            return SharedSanitizer.Value.Sanitize(html);
        }

        private static HtmlSanitizer CreateSanitizer()
        {
            var policy = GcpSecretManagerPolicyProvider.Load() ?? SanitizerPolicy.Default();
            var sanitizer = new HtmlSanitizer();

            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedSchemes.Clear();

            if (policy.AllowedTags != null)
                foreach (var t in policy.AllowedTags) sanitizer.AllowedTags.Add(t);
            if (policy.AllowedAttributes != null)
                foreach (var a in policy.AllowedAttributes) sanitizer.AllowedAttributes.Add(a);
            if (policy.AllowedSchemes != null)
                foreach (var s in policy.AllowedSchemes) sanitizer.AllowedSchemes.Add(s);

            return sanitizer;
        }
    }
}
