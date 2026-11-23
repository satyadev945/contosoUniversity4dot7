using Ganss.Xss;

namespace ContosoUniversity.Security
{
    public interface IHtmlSanitizerService
    {
        string Sanitize(string html);
    }

    /// <summary>
    /// Sanitizes untrusted HTML using Ganss.Xss.HtmlSanitizer with a policy sourced from Azure Key Vault.
    /// Use this instead of @Html.Raw()/HtmlString for any markup that must be rendered unencoded.
    /// </summary>
    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        private static readonly HtmlSanitizer Sanitizer = Build(HtmlSanitizerPolicy.Load());

        private static HtmlSanitizer Build(HtmlSanitizerPolicy policy)
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedSchemes.Clear();
            foreach (var t in policy.AllowedTags) sanitizer.AllowedTags.Add(t);
            foreach (var a in policy.AllowedAttributes) sanitizer.AllowedAttributes.Add(a);
            foreach (var s in policy.AllowedSchemes) sanitizer.AllowedSchemes.Add(s);
            return sanitizer;
        }

        public string Sanitize(string html)
        {
            return string.IsNullOrEmpty(html) ? html : Sanitizer.Sanitize(html);
        }
    }
}
