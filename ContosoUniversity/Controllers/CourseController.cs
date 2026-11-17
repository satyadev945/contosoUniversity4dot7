using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Controllers
{
    public class CourseController : Controller
    {
        private readonly SchoolContext _context;
        private readonly ILogger<CourseController> _logger;

        public CourseController(SchoolContext context, ILogger<CourseController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Course
        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                .Include(c => c.Department)
                .AsNoTracking()
                .ToListAsync();
            return View(courses);
        }

        // GET: Course/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            var course = await _context.Courses
                .Include(c => c.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseID == id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // GET: Course/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDepartmentsDropDownListAsync();
            return View();
        }

        // POST: Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseID,Title,Credits,DepartmentID")] Course course)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Courses.Add(course);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Unable to create course {CourseID}", course.CourseID);
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }

            await PopulateDepartmentsDropDownListAsync(course.DepartmentID);
            return View(course);
        }

        // GET: Course/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            await PopulateDepartmentsDropDownListAsync(course.DepartmentID);
            return View(course);
        }

        // POST: Course/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("CourseID,Title,Credits,DepartmentID")] Course course)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Entry(course).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Concurrency error while editing course {CourseID}", course.CourseID);
                    if (!await _context.Courses.AnyAsync(c => c.CourseID == course.CourseID))
                    {
                        return NotFound();
                    }
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Unable to edit course {CourseID}", course.CourseID);
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }
            await PopulateDepartmentsDropDownListAsync(course.DepartmentID);
            return View(course);
        }

        // GET: Course/Delete/5
        public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false)
        {
            if (id == null)
            {
                return BadRequest();
            }
            var course = await _context.Courses
                .Include(c => c.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseID == id);
            if (course == null)
            {
                return NotFound();
            }
            if (saveChangesError.GetValueOrDefault())
            {
                ViewBag.ErrorMessage = "Delete failed. Try again, and if the problem persists see your system administrator.";
            }
            return View(course);
        }

        // POST: Course/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return RedirectToAction(nameof(Index));
            }
            try
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Unable to delete course {CourseID}", id);
                return RedirectToAction(nameof(Delete), new { id, saveChangesError = true });
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDepartmentsDropDownListAsync(object? selectedDepartment = null)
        {
            var departments = await _context.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
            ViewBag.DepartmentID = new SelectList(departments, "DepartmentID", "Name", selectedDepartment);
        }
    }
}
