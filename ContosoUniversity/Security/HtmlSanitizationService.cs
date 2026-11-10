using System;
using Ganss.Xss;

namespace ContosoUniversity.Security
{
    public interface IHtmlSanitizationService
    {
        string Sanitize(string html);
    }

    /// <summary>
    /// Sanitizes untrusted HTML with Ganss.Xss.HtmlSanitizer using a policy
    /// retrieved from Azure Key Vault. Replaces unsafe Html.Raw / HtmlString usage.
    /// </summary>
    public class HtmlSanitizationService : IHtmlSanitizationService
    {
        private static readonly Lazy<HtmlSanitizer> SharedSanitizer = new Lazy<HtmlSanitizer>(CreateSanitizer);
        private static readonly object SyncRoot = new object();

        public static IHtmlSanitizationService Instance { get; } = new HtmlSanitizationService();

        public string Sanitize(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }

            // HtmlSanitizer instances are not guaranteed thread-safe; serialize access.
            lock (SyncRoot)
            {
                return SharedSanitizer.Value.Sanitize(html);
            }
        }

        private static HtmlSanitizer CreateSanitizer()
        {
            var policy = SanitizationPolicyProvider.Load();
            var sanitizer = new HtmlSanitizer();

            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedSchemes.Clear();
            sanitizer.AllowedCssProperties.Clear();

            foreach (var t in policy.AllowedTags) sanitizer.AllowedTags.Add(t);
            foreach (var a in policy.AllowedAttributes) sanitizer.AllowedAttributes.Add(a);
            foreach (var s in policy.AllowedSchemes) sanitizer.AllowedSchemes.Add(s);
            foreach (var c in policy.AllowedCssProperties) sanitizer.AllowedCssProperties.Add(c);

            return sanitizer;
        }
    }
}
