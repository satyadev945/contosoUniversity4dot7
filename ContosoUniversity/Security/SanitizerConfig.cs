using System.Collections.Generic;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Allowed HTML tags/attributes/schemes used by Ganss.Xss.HtmlSanitizer.
    /// The document is stored centrally in AWS Secrets Manager (see HtmlSanitizerProvider) so it can be
    /// managed once for all tenants. All lists are optional; an empty/missing list means "allow nothing"
    /// for tags and attributes (plain text only).
    /// </summary>
    public class SanitizerConfig
    {
        public List<string> AllowedTags { get; set; } = new List<string>();
        public List<string> AllowedAttributes { get; set; } = new List<string>();
        public List<string> AllowedSchemes { get; set; } = new List<string> { "http", "https", "mailto" };
    }
}
