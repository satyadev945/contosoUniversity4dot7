using System.Collections.Generic;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Allow-list configuration for Ganss.Xss.HtmlSanitizer. The JSON representation of this class
    /// is stored in AWS Secrets Manager (secret id from the HTML_SANITIZER_SECRET_ID environment
    /// variable) so that it can be managed centrally across multi-tenant environments.
    /// Example secret value:
    /// {"AllowedTags":["b","i","em","strong"],"AllowedAttributes":["class"],"AllowedSchemes":["https"],"AllowedCssProperties":[]}
    /// </summary>
    public class HtmlSanitizerConfig
    {
        public List<string> AllowedTags { get; set; }

        public List<string> AllowedAttributes { get; set; }

        public List<string> AllowedSchemes { get; set; }

        public List<string> AllowedCssProperties { get; set; }

        /// <summary>Most restrictive default: no tags / attributes allowed, https only.</summary>
        public static HtmlSanitizerConfig Default()
        {
            return new HtmlSanitizerConfig
            {
                AllowedTags = new List<string>(),
                AllowedAttributes = new List<string>(),
                AllowedSchemes = new List<string> { "https" },
                AllowedCssProperties = new List<string>()
            };
        }
    }
}
