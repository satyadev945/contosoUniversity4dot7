using System.Collections.Generic;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Allowed HTML tags/attributes/schemes for the Ganss.Xss HtmlSanitizer.
    /// The authoritative copy is stored in AWS Secrets Manager (secret id taken from the
    /// HTML_SANITIZER_SECRET_ID environment variable) so it can be managed centrally across tenants.
    /// </summary>
    public class HtmlSanitizationConfig
    {
        public List<string> AllowedTags { get; set; } = new List<string> { "b", "i", "em", "strong", "p", "br", "ul", "ol", "li" };
        public List<string> AllowedAttributes { get; set; } = new List<string> { "class" };
        public List<string> AllowedSchemes { get; set; } = new List<string> { "http", "https", "mailto" };
    }
}
