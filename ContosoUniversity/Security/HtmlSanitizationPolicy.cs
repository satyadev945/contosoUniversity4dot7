using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Allowed tags/attributes policy for HtmlSanitizer. Stored as a JSON secret in Azure Key Vault
    /// (secret name configured by HTML_SANITIZER_POLICY_SECRET, default "HtmlSanitizerPolicy").
    /// Example: {"AllowedTags":["b","i","em","strong","p","br"],"AllowedAttributes":["class"],"AllowedSchemes":["https"]}
    /// An empty/missing policy means no markup is allowed (all tags stripped).
    /// </summary>
    [DataContract]
    public class HtmlSanitizationPolicy
    {
        [DataMember] public List<string> AllowedTags { get; set; } = new List<string>();
        [DataMember] public List<string> AllowedAttributes { get; set; } = new List<string>();
        [DataMember] public List<string> AllowedSchemes { get; set; } = new List<string> { "https" };
    }
}
