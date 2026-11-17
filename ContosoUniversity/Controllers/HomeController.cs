using ContosoUniversity.Caching;
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
        private readonly IDataCache cache = new RedisDataCache();

        public const string EnrollmentStatsCacheKey = "home:enrollment-stats";

        public Task<ActionResult> Index()
        {
            return Task.FromResult<ActionResult>(View());
        }

        public async Task<ActionResult> About()
        {
            // Aggregated statistics are served from Azure Cache for Redis; the database query
            // runs asynchronously only on a cache miss.
            var data = await cache.GetOrCreateAsync(EnrollmentStatsCacheKey, async () =>
                await (from student in db.Students
                       group student by student.EnrollmentDate into dataGroup
                       select new EnrollmentDataGroup()
                       {
                           EnrollmentDate = dataGroup.Key,
                           StudentCount = dataGroup.Count()
                       }).ToListAsync(),
                TimeSpan.FromMinutes(5));

            return View(data);
        }

        public Task<ActionResult> Contact()
        {
            ViewBag.Message = HtmlSanitization.Sanitize("Your contact page.");

            return Task.FromResult<ActionResult>(View());
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}