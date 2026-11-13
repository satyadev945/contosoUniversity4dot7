using ContosoUniversity.DAL;
using ContosoUniversity.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace ContosoUniversity.Controllers
{
    public class HomeController : Controller
    {

        private SchoolContext db = new SchoolContext();

        public Task<ActionResult> Index()
        {
            return Task.FromResult<ActionResult>(View());
        }

        public async Task<ActionResult> About()
        {
            IQueryable<EnrollmentDataGroup> data = db.Students
                .GroupBy(student => student.EnrollmentDate)
                .Select(dataGroup => new EnrollmentDataGroup()
                {
                    EnrollmentDate = dataGroup.Key,
                    StudentCount = dataGroup.Count()
                });

            return View(await data.ToListAsync());
        }

        public Task<ActionResult> Contact()
        {
            ViewBag.Message = "Your contact page.";

            return Task.FromResult<ActionResult>(View());
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}