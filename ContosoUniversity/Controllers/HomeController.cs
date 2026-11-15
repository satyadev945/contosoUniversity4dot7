using ContosoUniversity.DAL;
using ContosoUniversity.ViewModels;
using ContosoUniversity.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;

namespace ContosoUniversity.Controllers
{
    public class HomeController : Controller
    {

        private SchoolContext db = new SchoolContext();
        private readonly ICacheService cache = new RedisCacheService();

        public Task<ActionResult> Index()
        {
            return Task.FromResult<ActionResult>(View());
        }

        public async Task<ActionResult> About()
        {
            // Aggregated statistics cached briefly in GCP Memorystore (Redis); falls back to DB.
            var data = await cache.GetOrSetAsync("stats:enrollment-by-date", () =>
            {
                // EF Core query (method syntax); translated to PostgreSQL via Npgsql provider.
                IQueryable<EnrollmentDataGroup> query = db.Students
                    .AsNoTracking()
                    .GroupBy(student => student.EnrollmentDate)
                    .Select(dataGroup => new EnrollmentDataGroup
                    {
                        EnrollmentDate = dataGroup.Key,
                        StudentCount = dataGroup.Count()
                    });
                return query.ToListAsync();
            }, TimeSpan.FromSeconds(30));

            return View(data);
        }

        public Task<ActionResult> Contact()
        {
            ViewBag.Message = HtmlSanitizerProvider.Sanitize("Your contact page.");

            return Task.FromResult<ActionResult>(View());
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}