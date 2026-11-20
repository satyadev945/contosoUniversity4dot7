using System.Collections.Generic;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Allowed HTML tags / attributes / schemes used by the HtmlSanitizer. The values are stored centrally
    /// in AWS Secrets Manager (secret id taken from the HTML_SANITIZER_SECRET_ID environment variable) as JSON:
    /// { "AllowedTags": ["b","i"], "AllowedAttributes": ["class"], "AllowedSchemes": ["https"] }
    /// </summary>
    public class HtmlSanitizerOptions
    {
        public List<string> AllowedTags { get; set; } = new List<string> { "b", "i", "em", "strong", "p", "br", "ul", "ol", "li" };
        public List<string> AllowedAttributes { get; set; } = new List<string> { "class" };
        public List<string> AllowedSchemes { get; set; } = new List<string> { "https" };
    }
}
