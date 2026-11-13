using System.Linq;
using System.Web.Mvc;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Global result filter that runs every string value placed in ViewBag through the
    /// Ganss.Xss HtmlSanitizer before the view is rendered.
    /// </summary>
    public class SanitizeViewBagAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            var viewData = filterContext.Controller.ViewData;
            foreach (var key in viewData.Keys.ToList())
            {
                var text = viewData[key] as string;
                if (text != null)
                {
                    viewData[key] = HtmlSanitizerService.Sanitize(text);
                }
            }
            base.OnResultExecuting(filterContext);
        }
    }
}
