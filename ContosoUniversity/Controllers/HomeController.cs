using ContosoUniversity.DAL;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

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

        public IActionResult About()
        {
            var data = (from student in db.Students.AsNoTracking()
                        group student by student.EnrollmentDate into dataGroup
                        select new EnrollmentDataGroup()
                        {
                            EnrollmentDate = dataGroup.Key,
                            StudentCount = dataGroup.Count()
                        }).ToList();

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
