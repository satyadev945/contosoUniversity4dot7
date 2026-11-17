using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using X.PagedList;
using X.PagedList.EF;

namespace ContosoUniversity.Controllers
{
    public class StudentsController : Controller
    {
        private readonly SchoolContext _context;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(SchoolContext context, ILogger<StudentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Students
        public async Task<IActionResult> Index(string? sortOrder, string? currentFilter, string? searchString, int? page)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            IQueryable<Student> students = _context.Students.AsNoTracking();

            if (!string.IsNullOrEmpty(searchString))
            {
                students = students.Where(s => s.LastName.Contains(searchString)
                                           || s.FirstMidName.Contains(searchString));
            }

            students = sortOrder switch
            {
                "name_desc" => students.OrderByDescending(s => s.LastName),
                "Date" => students.OrderBy(s => s.EnrollmentDate),
                "date_desc" => students.OrderByDescending(s => s.EnrollmentDate),
                _ => students.OrderBy(s => s.LastName),
            };

            int pageSize = 3;
            int pageNumber = page ?? 1;
            return View(await students.ToPagedListAsync(pageNumber, pageSize));
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            var student = await _context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ID == id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LastName,FirstMidName,EnrollmentDate")] Student student)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is RetryLimitExceededException)
            {
                _logger.LogError(ex, "Unable to create student");
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }

            return View(student);
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // POST: Students/Edit/5
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            var studentToUpdate = await _context.Students.FirstOrDefaultAsync(s => s.ID == id);
            if (studentToUpdate == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync(studentToUpdate, "",
                s => s.LastName, s => s.FirstMidName, s => s.EnrollmentDate))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex) when (ex is DbUpdateException || ex is RetryLimitExceededException)
                {
                    _logger.LogError(ex, "Unable to edit student {ID}", id);
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return View(studentToUpdate);
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false)
        {
            if (id == null)
            {
                return BadRequest();
            }
            if (saveChangesError.GetValueOrDefault())
            {
                ViewBag.ErrorMessage = "Delete failed. Try again, and if the problem persists see your system administrator.";
            }
            var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.ID == id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student != null)
                {
                    _context.Students.Remove(student);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is RetryLimitExceededException)
            {
                _logger.LogError(ex, "Unable to delete student {ID}", id);
                return RedirectToAction(nameof(Delete), new { id, saveChangesError = true });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
