using System.Collections.Generic;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Sanitization policy stored as a versioned secret in GCP Secret Manager (JSON).
    /// Example: {"allowedTags":["b","i","p"],"allowedAttributes":["class"],"allowedSchemes":["https"]}
    /// </summary>
    public class SanitizerPolicy
    {
        public List<string> AllowedTags { get; set; }
        public List<string> AllowedAttributes { get; set; }
        public List<string> AllowedSchemes { get; set; }

        /// <summary>Restrictive fallback used when the secret is unavailable.</summary>
        public static SanitizerPolicy Default()
        {
            return new SanitizerPolicy
            {
                AllowedTags = new List<string> { "b", "i", "em", "strong", "p", "br", "ul", "ol", "li" },
                AllowedAttributes = new List<string>(),
                AllowedSchemes = new List<string> { "https" }
            };
        }
    }
}
