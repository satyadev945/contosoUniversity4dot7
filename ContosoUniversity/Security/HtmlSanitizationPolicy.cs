using System.Collections.Generic;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Allowed-markup policy used by the Ganss.Xss HtmlSanitizer.
    /// The policy is stored as a JSON secret in Azure Key Vault (secret name
    /// "HtmlSanitizerPolicy" by default) so it can be managed centrally for
    /// multi-tenant Azure SaaS deployments. Example secret value:
    /// {
    ///   "AllowedTags": ["b","i","em","strong","p","br"],
    ///   "AllowedAttributes": ["class"],
    ///   "AllowedSchemes": ["https"],
    ///   "AllowedCssProperties": []
    /// }
    /// </summary>
    public class HtmlSanitizationPolicy
    {
        public List<string> AllowedTags { get; set; } = new List<string>();
        public List<string> AllowedAttributes { get; set; } = new List<string>();
        public List<string> AllowedSchemes { get; set; } = new List<string>();
        public List<string> AllowedCssProperties { get; set; } = new List<string>();

        /// <summary>
        /// Fail-closed default: no markup is allowed (plain text only).
        /// </summary>
        public static HtmlSanitizationPolicy StrictDefault()
        {
            return new HtmlSanitizationPolicy
            {
                AllowedSchemes = new List<string> { "https" }
            };
        }
    }
}
