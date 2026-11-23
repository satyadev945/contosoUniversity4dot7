using System.Collections.Generic;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Allow-list configuration for the Ganss.Xss HtmlSanitizer.
    /// The authoritative copy is stored in AWS Secrets Manager (secret id provided through the
    /// HTML_SANITIZER_SECRET_ID environment variable) so it can be managed centrally across tenants.
    /// </summary>
    public class HtmlSanitizerConfig
    {
        public List<string> AllowedTags { get; set; }
        public List<string> AllowedAttributes { get; set; }
        public List<string> AllowedSchemes { get; set; }
        public List<string> AllowedCssProperties { get; set; }

        /// <summary>Restrictive defaults used when the secret is not configured or cannot be read.</summary>
        public static HtmlSanitizerConfig Default()
        {
            return new HtmlSanitizerConfig
            {
                AllowedTags = new List<string> { "b", "i", "em", "strong", "u", "p", "br", "ul", "ol", "li", "span" },
                AllowedAttributes = new List<string> { "class" },
                AllowedSchemes = new List<string> { "http", "https" },
                AllowedCssProperties = new List<string>()
            };
        }
    }
}
