namespace ContosoUniversity.Security
{
    /// <summary>
    /// Sanitizes untrusted HTML/text before it is stored or echoed back into a view.
    /// Replaces any use of Html.Raw()/HtmlString on user-controlled content.
    /// </summary>
    public interface IHtmlSanitizerService
    {
        string Sanitize(string html);
    }
}
