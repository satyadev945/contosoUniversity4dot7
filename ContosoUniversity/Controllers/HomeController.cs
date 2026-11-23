using ContosoUniversity.DAL;
using ContosoUniversity.Security;
using ContosoUniversity.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;

namespace ContosoUniversity.Controllers
{
    public class HomeController : Controller
    {

        private SchoolContext db = new SchoolContext();
        private readonly IHtmlSanitizerService sanitizer = new HtmlSanitizerService();

        public Task<ActionResult> Index()
        {
            return Task.FromResult<ActionResult>(View());
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

        public Task<ActionResult> Contact()
        {
            ViewBag.Message = sanitizer.Sanitize("Your contact page.");

            return Task.FromResult<ActionResult>(View());
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}