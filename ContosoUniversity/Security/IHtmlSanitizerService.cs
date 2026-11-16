namespace ContosoUniversity.Security
{
    /// <summary>
    /// Sanitizes untrusted HTML so it can be safely rendered. Replaces any use of
    /// Html.Raw / HtmlString on untrusted content.
    /// </summary>
    public interface IHtmlSanitizerService
    {
        /// <summary>Returns sanitized HTML (never null).</summary>
        string Sanitize(string html);
    }
}
