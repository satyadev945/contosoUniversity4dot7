using System.Collections.Generic;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Allowed HTML tags/attributes/schemes used by the HtmlSanitizer.
    /// Loaded from AWS Secrets Manager (secret id in env var SANITIZER_CONFIG_SECRET_ID).
    /// Expected JSON: {"allowedTags":["b","i"],"allowedAttributes":["class"],"allowedSchemes":["https"]}
    /// </summary>
    public class SanitizerConfig
    {
        public List<string> AllowedTags { get; set; }
        public List<string> AllowedAttributes { get; set; }
        public List<string> AllowedSchemes { get; set; }
    }
}
