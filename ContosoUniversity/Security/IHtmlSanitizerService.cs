namespace ContosoUniversity.Security
{
    public interface IHtmlSanitizerService
    {
        /// <summary>Returns HTML with everything outside the configured allow-list removed.</summary>
        string SanitizeHtml(string input);

        /// <summary>
        /// Returns plain text with all markup removed (sanitised, then HTML-decoded) so that Razor's automatic
        /// HTML encoding can safely be applied on output without double encoding.
        /// </summary>
        string SanitizeText(string input);
    }
}
