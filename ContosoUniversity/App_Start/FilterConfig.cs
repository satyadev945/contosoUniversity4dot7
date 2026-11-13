using System.Web;
using System.Web.Mvc;
using ContosoUniversity.Security;

namespace ContosoUniversity
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new SanitizeViewBagAttribute());
        }
    }
}
