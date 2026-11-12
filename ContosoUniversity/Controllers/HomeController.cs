using System.Diagnostics;
using ContosoUniversity.DAL;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Controllers
{
    public class HomeController : Controller
    {
        private readonly SchoolContext _db;
        private readonly ILogger<HomeController> _logger;

        public HomeController(SchoolContext db, ILogger<HomeController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> About()
        {
            // EF Core GroupBy translated to PostgreSQL (no LINQ to SQL constructs)
            var data = _db.Students.AsNoTracking()
                .GroupBy(s => s.EnrollmentDate)
                .Select(g => new EnrollmentDataGroup
                {
                    EnrollmentDate = g.Key,
                    StudentCount = g.Count()
                })
                .OrderBy(x => x.EnrollmentDate);

            return View(await data.ToListAsync());
        }

        public IActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogError("Unhandled error for request {RequestId}", Activity.Current?.Id ?? HttpContext.TraceIdentifier);
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult HttpStatus(int code)
        {
            Response.StatusCode = code;
            ViewBag.StatusCode = code;
            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
