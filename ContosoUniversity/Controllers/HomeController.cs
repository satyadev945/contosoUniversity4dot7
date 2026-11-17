using ContosoUniversity.DAL;
using ContosoUniversity.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ContosoUniversity.Controllers
{
    public class HomeController : Controller
    {

        private SchoolContext db = new SchoolContext();

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

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}