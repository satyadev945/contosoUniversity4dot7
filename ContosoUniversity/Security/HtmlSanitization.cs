using System;
using System.Net;
using Ganss.Xss;

namespace ContosoUniversity.Security
{
    public interface IHtmlSanitizerService
    {
        /// <summary>Returns sanitized HTML (allowed markup per policy, everything else removed).</summary>
        string SanitizeHtml(string input);

        /// <summary>
        /// Returns sanitized text suitable for rendering through Razor's automatic HTML
        /// encoding (@value). Markup is removed per policy and the result is decoded so
        /// Razor does not double-encode it.
        /// </summary>
        string SanitizeText(string input);
    }

    /// <summary>
    /// Ganss.Xss HtmlSanitizer based implementation. The allowed tags/attributes policy
    /// comes from Azure Key Vault (see <see cref="HtmlSanitizationPolicyProvider"/>).
    /// </summary>
    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        private readonly HtmlSanitizer _sanitizer;

        public HtmlSanitizerService(HtmlSanitizationPolicy policy)
        {
            policy = policy ?? HtmlSanitizationPolicy.StrictDefault();

            _sanitizer = new HtmlSanitizer();
            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedAttributes.Clear();
            _sanitizer.AllowedSchemes.Clear();
            _sanitizer.AllowedCssProperties.Clear();

            foreach (var t in policy.AllowedTags ?? new System.Collections.Generic.List<string>()) _sanitizer.AllowedTags.Add(t);
            foreach (var a in policy.AllowedAttributes ?? new System.Collections.Generic.List<string>()) _sanitizer.AllowedAttributes.Add(a);
            foreach (var s in policy.AllowedSchemes ?? new System.Collections.Generic.List<string>()) _sanitizer.AllowedSchemes.Add(s);
            foreach (var c in policy.AllowedCssProperties ?? new System.Collections.Generic.List<string>()) _sanitizer.AllowedCssProperties.Add(c);
        }

        public string SanitizeHtml(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // HtmlSanitizer instances are not guaranteed thread-safe for concurrent configuration;
            // sanitizing is serialized to be safe.
            lock (_sanitizer)
            {
                return _sanitizer.Sanitize(input);
            }
        }

        public string SanitizeText(string input)
        {
            string sanitized = SanitizeHtml(input);
            return string.IsNullOrEmpty(sanitized) ? sanitized : WebUtility.HtmlDecode(sanitized);
        }
    }

    /// <summary>
    /// Process-wide access point for the sanitizer (controllers and Razor views).
    /// </summary>
    public static class HtmlSanitization
    {
        private static readonly Lazy<IHtmlSanitizerService> LazyInstance =
            new Lazy<IHtmlSanitizerService>(
                () => new HtmlSanitizerService(HtmlSanitizationPolicyProvider.Load()),
                true);

        public static IHtmlSanitizerService Current
        {
            get { return LazyInstance.Value; }
        }

        /// <summary>Eagerly loads the policy (call during application start-up).</summary>
        public static void Initialize()
        {
            var ignored = Current;
        }

        public static string SanitizeHtml(string input)
        {
            return Current.SanitizeHtml(input);
        }

        public static string SanitizeText(string input)
        {
            return Current.SanitizeText(input);
        }
    }
}
