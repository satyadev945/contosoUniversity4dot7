using System.Diagnostics;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Controllers
{
    public class HomeController : Controller
    {
        private readonly SchoolContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(SchoolContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Data access uses EF Core (SchoolContext) only; no LINQ to SQL APIs are used.
            return View();
        }

        public async Task<IActionResult> About()
        {
            var data = await _context.Students
                .AsNoTracking()
                .GroupBy(student => student.EnrollmentDate)
                .Select(dataGroup => new EnrollmentDataGroup
                {
                    EnrollmentDate = dataGroup.Key,
                    StudentCount = dataGroup.Count()
                })
                .ToListAsync();

            return View(data);
        }

        public IActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Unhandled error. RequestId: {RequestId}", requestId);
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
