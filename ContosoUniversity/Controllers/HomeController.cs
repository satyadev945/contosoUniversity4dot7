using System.Linq;
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

        public IActionResult About()
        {
            IQueryable<EnrollmentDataGroup> data = from student in db.Students.AsNoTracking()
                                                   group student by student.EnrollmentDate into dataGroup
                                                   select new EnrollmentDataGroup()
                                                   {
                                                       EnrollmentDate = dataGroup.Key,
                                                       StudentCount = dataGroup.Count()
                                                   };

            return View(data.ToList());
        }

        public IActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
