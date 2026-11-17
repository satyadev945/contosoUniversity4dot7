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
using PagedList;
using System.Data.Entity.Infrastructure;
using ContosoUniversity.Services;

namespace ContosoUniversity.Controllers
{
    public class StudentsController : Controller
    {
        private SchoolContext db = new SchoolContext();
        private readonly IStudentCache _studentCache;

        public StudentsController(IStudentCache studentCache)
        {
            _studentCache = studentCache;
        }

        // Async cache-aside read (Azure Cache for Redis, then DB). Navigation data needed by the
        // views (Enrollments -> Course) is eager loaded so views never trigger blocking lazy loads.
        private async Task<Student> GetStudentForViewAsync(int id)
        {
            var cached = await _studentCache.GetAsync(id);
            if (cached != null)
            {
                return cached;
            }
            var student = await db.Students
                .Include(s => s.Enrollments.Select(e => e.Course))
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.ID == id);
            await _studentCache.SetAsync(student);
            return student;
        }

        // GET: Students
        public async Task<ActionResult> Index(string sortOrder, string currentFilter,  string searchString, int? page)
        {
            ViewBag.Currentsort = HtmlSanitizerService.Instance.SanitizeText(sortOrder);
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            // Anti-XSS: user-supplied filter text is sanitized (policy from Azure Key Vault) before reaching the view.
            ViewBag.CurrentFilter = HtmlSanitizerService.Instance.SanitizeText(searchString);

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
            int totalCount = await students.CountAsync();
            var pageItems = await students.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return View(new StaticPagedList<Student>(pageItems, pageNumber, pageSize, totalCount));
        }

        // GET: Students/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Student student = await GetStudentForViewAsync(id.Value);
            if (student == null)
            {
                return HttpNotFound();
            }
            return View(student);
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


            return View(student);
        }

        // GET: Students/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Student student = await GetStudentForViewAsync(id.Value);
            if (student == null)
            {
                return HttpNotFound();
            }
            return View(student);
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
                    await _studentCache.RemoveAsync(id.Value);

                    return RedirectToAction("Index");
                }
                catch (RetryLimitExceededException/* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return View(studentToUpdate);
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
                ViewBag.ErrorMessage = HtmlSanitizerService.Instance.SanitizeText("Delete failed. Try again, and if the problem persists see your system administrator.");
            }
            Student student = await GetStudentForViewAsync(id.Value);
            if (student == null)
            {
                return HttpNotFound();
            }
            return View(student);
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
                await _studentCache.RemoveAsync(id);
            }
            catch (Exception)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                return RedirectToAction("Delete", new { id = id, saveChangesError = true });
            }
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
