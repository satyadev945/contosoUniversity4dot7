using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ContosoUniversity.Caching;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Security;

namespace ContosoUniversity.Controllers
{
    public class CourseController : Controller
    {
        private SchoolContext db = new SchoolContext();
        private readonly ICacheService cache = RedisCacheService.Instance;
        private readonly IHtmlSanitizerService sanitizer = HtmlSanitizerService.Instance;

        private const string DepartmentsCacheKey = "contoso:departments:lookup";
        private static readonly TimeSpan DepartmentsCacheTtl = TimeSpan.FromMinutes(5);

        // Reads the department lookup from AWS ElastiCache (Redis); falls back to the database on a miss.
        private async Task<SelectList> GetDepartmentSelectListAsync(object selectedValue = null)
        {
            var departments = await cache.GetAsync<List<DepartmentLookup>>(DepartmentsCacheKey);
            if (departments == null)
            {
                departments = await db.Departments
                    .OrderBy(d => d.Name)
                    .Select(d => new DepartmentLookup { DepartmentID = d.DepartmentID, Name = d.Name })
                    .ToListAsync();
                await cache.SetAsync(DepartmentsCacheKey, departments, DepartmentsCacheTtl);
            }
            return new SelectList(departments, "DepartmentID", "Name", selectedValue);
        }

        // GET: Course
        public async Task<ActionResult> Index()
        {
            var courses = db.Courses.Include(c => c.Department);
            return View(await courses.ToListAsync());
        }

        // GET: Course/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Course course = await db.Courses.FindAsync(id);
            if (course == null)
            {
                return HttpNotFound();
            }
            return View(course);
        }

        // GET: Course/Create
        public async Task<ActionResult> Create()
        {
            ViewBag.DepartmentID = await GetDepartmentSelectListAsync();
            return View();
        }

        // POST: Course/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "CourseID,Title,Credits,DepartmentID")] Course course)
        {
            course.Title = sanitizer.Sanitize(course.Title);
            if (ModelState.IsValid)
            {
                db.Courses.Add(course);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.DepartmentID = await GetDepartmentSelectListAsync(course.DepartmentID);
            return View(course);
        }

        // GET: Course/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Course course = await db.Courses.FindAsync(id);
            if (course == null)
            {
                return HttpNotFound();
            }
            ViewBag.DepartmentID = await GetDepartmentSelectListAsync(course.DepartmentID);
            return View(course);
        }

        // POST: Course/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "CourseID,Title,Credits,DepartmentID")] Course course)
        {
            course.Title = sanitizer.Sanitize(course.Title);
            if (ModelState.IsValid)
            {
                db.Entry(course).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.DepartmentID = await GetDepartmentSelectListAsync(course.DepartmentID);
            return View(course);
        }

        // GET: Course/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Course course = await db.Courses.FindAsync(id);
            if (course == null)
            {
                return HttpNotFound();
            }
            return View(course);
        }

        // POST: Course/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Course course = await db.Courses.FindAsync(id);
            db.Courses.Remove(course);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
