using System.Threading.Tasks;

namespace ContosoUniversity.Services
{
    /// <summary>
    /// Sanitizes user-controlled strings (using Ganss.Xss HtmlSanitizer) before they are
    /// rendered by Razor views. Replaces any use of Html.Raw / HtmlString.
    /// </summary>
    public interface IHtmlSanitizerService
    {
        Task<string> SanitizeAsync(string html);

        /// <summary>
        /// Sanitizes all public writable string properties of the supplied model (or of each item
        /// when the model is a collection) and returns the same instance.
        /// </summary>
        Task<T> SanitizeModelAsync<T>(T model);
    }
}
