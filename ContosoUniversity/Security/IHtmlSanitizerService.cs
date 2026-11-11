namespace ContosoUniversity.Security
{
    public interface IHtmlSanitizerService
    {
        /// <summary>Returns the input with all disallowed markup removed (null-safe).</summary>
        string Sanitize(string html);
    }
}
