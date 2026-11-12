using ContosoUniversity.DAL;
using ContosoUniversity.Security;
using ContosoUniversity.ViewModels;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
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
            // EF Core query (translated to PostgreSQL by Npgsql provider)
            IQueryable<EnrollmentDataGroup> data = db.Students
                .AsNoTracking()
                .GroupBy(student => student.EnrollmentDate)
                .Select(dataGroup => new EnrollmentDataGroup
                {
                    EnrollmentDate = dataGroup.Key,
                    StudentCount = dataGroup.Count()
                });

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