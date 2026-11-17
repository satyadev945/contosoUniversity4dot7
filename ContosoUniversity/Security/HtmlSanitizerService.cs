using Ganss.Xss;

namespace ContosoUniversity.Security
{
    public interface IHtmlSanitizerService
    {
        string Sanitize(string html);
    }

    /// <summary>
    /// Ganss.Xss HtmlSanitizer wrapper configured from a policy stored in GCP Secret Manager.
    /// Use instead of Html.Raw()/HtmlString for any untrusted content.
    /// </summary>
    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        private readonly HtmlSanitizer _sanitizer;

        public HtmlSanitizerService(SanitizationPolicy policy)
        {
            _sanitizer = new HtmlSanitizer();
            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedAttributes.Clear();
            _sanitizer.AllowedSchemes.Clear();
            foreach (var t in policy.AllowedTags) _sanitizer.AllowedTags.Add(t);
            foreach (var a in policy.AllowedAttributes) _sanitizer.AllowedAttributes.Add(a);
            foreach (var s in policy.AllowedSchemes) _sanitizer.AllowedSchemes.Add(s);
        }

        public string Sanitize(string html)
        {
            return string.IsNullOrEmpty(html) ? html : _sanitizer.Sanitize(html);
        }
    }

    /// <summary>
    /// Static access for controllers that are not constructed via DI. Initialised from Startup.
    /// </summary>
    public static class HtmlSanitization
    {
        private static IHtmlSanitizerService _service;

        public static void Initialize(IHtmlSanitizerService service)
        {
            _service = service;
        }

        public static string Sanitize(string input)
        {
            if (_service == null)
            {
                _service = new HtmlSanitizerService(SanitizationPolicyProvider.Load());
            }
            return _service.Sanitize(input);
        }
    }
}
