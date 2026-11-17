using ContosoUniversity.DAL;
using ContosoUniversity.Services;
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
        private readonly IHtmlSanitizerService sanitizer = new HtmlSanitizerService();

        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> About()
        {
            IQueryable<EnrollmentDataGroup> data = from student in db.Students.AsNoTracking()
                                                   group student by student.EnrollmentDate into dataGroup
                                                   select new EnrollmentDataGroup()
                                                   {
                                                       EnrollmentDate = dataGroup.Key,
                                                       StudentCount = dataGroup.Count()
                                                   };

            return View(await sanitizer.SanitizeModelAsync(await data.ToListAsync()));
        }

        public async Task<ActionResult> Contact()
        {
            ViewBag.Message = await sanitizer.SanitizeAsync("Your contact page.");

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}