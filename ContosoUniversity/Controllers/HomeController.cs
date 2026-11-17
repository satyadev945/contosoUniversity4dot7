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

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            var data = db.Students.AsNoTracking()
                .GroupBy(student => student.EnrollmentDate)
                .Select(dataGroup => new EnrollmentDataGroup()
                {
                    EnrollmentDate = dataGroup.Key,
                    StudentCount = dataGroup.Count()
                })
                .ToList();

            return View(data);
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Error()
        {
            return View();
        }
    }
}
