using System.Net;
using Ganss.Xss;

namespace ContosoUniversity.Security
{
    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        private readonly HtmlSanitizerOptions _options;

        public HtmlSanitizerService(HtmlSanitizerOptions options)
        {
            _options = options ?? new HtmlSanitizerOptions();
        }

        public string SanitizeHtml(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return CreateSanitizer(_options).Sanitize(input);
        }

        public string SanitizeText(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            // Empty allow-lists: strip every tag/attribute, then decode entities back to plain text
            // because Razor will HTML-encode on output.
            var sanitizer = CreateSanitizer(new HtmlSanitizerOptions
            {
                AllowedTags = new System.Collections.Generic.List<string>(),
                AllowedAttributes = new System.Collections.Generic.List<string>(),
                AllowedSchemes = new System.Collections.Generic.List<string>()
            });
            return WebUtility.HtmlDecode(sanitizer.Sanitize(input));
        }

        private static HtmlSanitizer CreateSanitizer(HtmlSanitizerOptions options)
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedSchemes.Clear();
            foreach (var t in options.AllowedTags) sanitizer.AllowedTags.Add(t);
            foreach (var a in options.AllowedAttributes) sanitizer.AllowedAttributes.Add(a);
            foreach (var s in options.AllowedSchemes) sanitizer.AllowedSchemes.Add(s);
            return sanitizer;
        }
    }
}
