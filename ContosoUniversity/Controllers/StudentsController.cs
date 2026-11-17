using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Services;
using PagedList;
using System.Data.Entity.Infrastructure;

namespace ContosoUniversity.Controllers
{
    public class StudentsController : Controller
    {
        private SchoolContext db = new SchoolContext();
        private readonly IHtmlSanitizerService sanitizer = new HtmlSanitizerService();

        // GET: Students
        public async Task<ActionResult> Index(string sortOrder, string currentFilter,  string searchString, int? page)
        {
            ViewBag.Currentsort = await sanitizer.SanitizeAsync(sortOrder);
            ViewBag.NameSortParm = await sanitizer.SanitizeAsync(String.IsNullOrEmpty(sortOrder) ? "name_desc" : "");
            ViewBag.DateSortParm = await sanitizer.SanitizeAsync(sortOrder == "Date" ? "date_desc" : "Date");

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = await sanitizer.SanitizeAsync(searchString);

            var students = from s in db.Students
                           select s;

            if (!string.IsNullOrEmpty(searchString))
            {
                students = students.Where(s => s.LastName.Contains(searchString)
                                           || s.FirstMidName.Contains(searchString));
                                            
            }
            switch (sortOrder)
            {
                case "name_desc":
                    students = students.OrderByDescending(s => s.LastName);
                    break;
                case "Date":
                    students = students.OrderBy(s => s.EnrollmentDate);
                    break;
                case "date_desc":
                    students = students.OrderByDescending(s => s.EnrollmentDate);
                    break;
                default:
                    students = students.OrderBy(s => s.LastName);
                    break;
            }

            int pageSize = 3;
            int pageNumber = (page ?? 1);
            // Non-blocking paging: async count + async page fetch
            int totalCount = await students.CountAsync();
            var pageItems = await students
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            await sanitizer.SanitizeModelAsync(pageItems);
            return View(new StaticPagedList<Student>(pageItems, pageNumber, pageSize, totalCount));
        }

        // GET: Students/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            // Eager-load Enrollments/Course asynchronously so the view never triggers
            // synchronous lazy-loading (blocking DB calls) while rendering.
            Student student = await LoadStudentForViewAsync(id.Value, includeEnrollments: true);
            if (student == null)
            {
                return HttpNotFound();
            }
            return View(await sanitizer.SanitizeModelAsync(student));
        }

        // GET: Students/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Students/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "LastName,FirstMidName,EnrollmentDate")] Student student)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Students.Add(student);
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }


            return View(await sanitizer.SanitizeModelAsync(student));
        }

        // GET: Students/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            // Fully materialized, detached entity: the view cannot trigger blocking lazy loads.
            Student student = await LoadStudentForViewAsync(id.Value, includeEnrollments: false);
            if (student == null)
            {
                return HttpNotFound();
            }
            return View(await sanitizer.SanitizeModelAsync(student));
        }

        // POST: Students/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditPost(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var studentToUpdate = await db.Students.FindAsync(id);
            if (TryUpdateModel(studentToUpdate, "",
               new string[] { "LastName", "FirstMidName", "EnrollmentDate" }))
            {
                try
                {
                    await db.SaveChangesAsync();

                    return RedirectToAction("Index");
                }
                catch (RetryLimitExceededException/* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return View(await sanitizer.SanitizeModelAsync(studentToUpdate));
        }

        // GET: Students/Delete/5
        public async Task<ActionResult> Delete(int? id, bool? saveChangesError = false)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (saveChangesError.GetValueOrDefault())
            {
                ViewBag.ErrorMessage = await sanitizer.SanitizeAsync("Delete failed. Try again, and if the problem persists see your system administrator.");
            }
            // Fully materialized, detached entity: the view cannot trigger blocking lazy loads.
            Student student = await LoadStudentForViewAsync(id.Value, includeEnrollments: false);
            if (student == null)
            {
                return HttpNotFound();
            }
            return View(await sanitizer.SanitizeModelAsync(student));
        }

        // POST: Students/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                Student student = await db.Students.FindAsync(id);
                db.Students.Remove(student);
                await db.SaveChangesAsync();
            }
            catch (Exception)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                return RedirectToAction("Delete", new { id = id, saveChangesError = true });
            }
            return RedirectToAction("Index");


        }




        // Async, non-blocking read of a student for view rendering. Data is fully loaded
        // before the view executes so Razor rendering performs no synchronous data access.
        private async Task<Student> LoadStudentForViewAsync(int id, bool includeEnrollments)
        {
            IQueryable<Student> query = db.Students.AsNoTracking();
            if (includeEnrollments)
            {
                query = query.Include(s => s.Enrollments.Select(e => e.Course));
            }
            return await query.FirstOrDefaultAsync(s => s.ID == id).ConfigureAwait(false);
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
