using System.Collections.Generic;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Allowed-markup configuration for the Ganss.Xss HtmlSanitizer. The JSON document is stored in
    /// AWS Secrets Manager (secret id taken from HTML_SANITIZER_SECRET_ID) so it can be managed
    /// centrally for all tenants/instances.
    /// </summary>
    public class HtmlSanitizerSettings
    {
        public List<string> AllowedTags { get; set; } = new List<string>();
        public List<string> AllowedAttributes { get; set; } = new List<string>();
        public List<string> AllowedSchemes { get; set; } = new List<string>();
        public List<string> AllowedCssProperties { get; set; } = new List<string>();

        /// <summary>
        /// Safe fallback (plain text only) used when no secret is configured or it cannot be read.
        /// </summary>
        public static HtmlSanitizerSettings Default()
        {
            return new HtmlSanitizerSettings
            {
                AllowedTags = new List<string>(),
                AllowedAttributes = new List<string>(),
                AllowedSchemes = new List<string> { "https" },
                AllowedCssProperties = new List<string>()
            };
        }
    }
}
