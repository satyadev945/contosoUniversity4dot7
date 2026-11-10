using System.Collections.Generic;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Allowed tags/attributes policy for HTML sanitization.
    /// Stored as a JSON secret in Azure Key Vault so it can be managed centrally
    /// across tenants. An empty policy means "strip all markup" (the safest default).
    /// </summary>
    public class SanitizationPolicy
    {
        public List<string> AllowedTags { get; set; } = new List<string>();
        public List<string> AllowedAttributes { get; set; } = new List<string>();
        public List<string> AllowedSchemes { get; set; } = new List<string> { "https" };
        public List<string> AllowedCssProperties { get; set; } = new List<string>();
    }
}
