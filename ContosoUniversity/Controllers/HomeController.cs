using System.Diagnostics;
using ContosoUniversity.DAL;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Controllers
{
    public class HomeController : Controller
    {
        private readonly SchoolContext db;

        public HomeController(SchoolContext context)
        {
            db = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> About()
        {
            var data = await (from student in db.Students
                              group student by student.EnrollmentDate into dataGroup
                              select new EnrollmentDataGroup()
                              {
                                  EnrollmentDate = dataGroup.Key,
                                  StudentCount = dataGroup.Count()
                              }).ToListAsync();

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
            return View();
        }
    }
}
