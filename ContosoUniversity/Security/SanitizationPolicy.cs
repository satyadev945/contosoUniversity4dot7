using System.Collections.Generic;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// HTML sanitization policy (allowed tags / attributes / URI schemes).
    /// Stored as a versioned JSON secret in GCP Secret Manager.
    /// The default policy is the strictest one: no tags, no attributes (plain text only).
    /// </summary>
    public class SanitizationPolicy
    {
        public List<string> AllowedTags { get; set; } = new List<string>();
        public List<string> AllowedAttributes { get; set; } = new List<string>();
        public List<string> AllowedSchemes { get; set; } = new List<string>();
    }
}
