using ContosoUniversity.DAL;
using ContosoUniversity.Security;
using ContosoUniversity.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ContosoUniversity.Controllers
{
    public class HomeController : Controller
    {

        private SchoolContext db = new SchoolContext();

        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> About()
        {
            IQueryable<EnrollmentDataGroup> data = from student in db.Students
                                                   group student by student.EnrollmentDate into dataGroup
                                                   select new EnrollmentDataGroup()
                                                   {
                                                       EnrollmentDate = dataGroup.Key,
                                                       StudentCount = dataGroup.Count()
                                                   };

            return View(await data.ToListAsync());
        }

        public ActionResult Contact()
        {
            ViewBag.Message = HtmlSanitizerProvider.SanitizeText("Your contact page.");

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}