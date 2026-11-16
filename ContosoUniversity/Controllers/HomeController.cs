using ContosoUniversity.DAL;
using ContosoUniversity.Services;
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
            // EF Core (Npgsql) query; no LINQ to SQL APIs (LoadWith/DataLoadOptions/ExecuteQuery) are used.
            var data = await db.Students.AsNoTracking()
                .GroupBy(student => student.EnrollmentDate)
                .Select(dataGroup => new EnrollmentDataGroup()
                {
                    EnrollmentDate = dataGroup.Key,
                    StudentCount = dataGroup.Count()
                })
                .ToListAsync();

            return View(data);
        }

        public ActionResult Contact()
        {
            ViewBag.Message = HtmlSanitizerService.Sanitize("Your contact page.");

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}