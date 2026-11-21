using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Controllers
{
    public class InstructorController : Controller
    {
        private readonly SchoolContext _db;
        private readonly ILogger<InstructorController> _logger;

        public InstructorController(SchoolContext db, ILogger<InstructorController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: Instructor
        public async Task<IActionResult> Index(int? id, int? courseID)
        {
            var viewModel = new InstructorIndexData();
            var instructors = await _db.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.Courses)
                    .ThenInclude(c => c.Department)
                .OrderBy(i => i.LastName)
                .ToListAsync();
            viewModel.Instructors = instructors;

            if (id != null)
            {
                ViewBag.InstructorID = id.Value;
                var selectedInstructor = instructors.SingleOrDefault(i => i.ID == id.Value);
                if (selectedInstructor == null)
                {
                    return NotFound();
                }
                viewModel.Courses = selectedInstructor.Courses;
            }

            if (courseID != null && viewModel.Courses != null)
            {
                ViewBag.CourseID = courseID.Value;
                var selectedCourse = viewModel.Courses.SingleOrDefault(x => x.CourseID == courseID);
                if (selectedCourse == null)
                {
                    return NotFound();
                }

                // Explicit loading
                await _db.Entry(selectedCourse).Collection(x => x.Enrollments).LoadAsync();
                foreach (Enrollment enrollment in selectedCourse.Enrollments)
                {
                    await _db.Entry(enrollment).Reference(x => x.Student).LoadAsync();
                }

                viewModel.Enrollments = selectedCourse.Enrollments;
            }

            return View(viewModel);
        }

        // GET: Instructor/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            var instructor = await _db.Instructors
                .Include(i => i.OfficeAssignment)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ID == id);
            if (instructor == null)
            {
                return NotFound();
            }
            return View(instructor);
        }

        // GET: Instructor/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Instructor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,LastName,FirstMidName,HireDate")] Instructor instructor)
        {
            if (ModelState.IsValid)
            {
                _db.Instructors.Add(instructor);
                try
                {
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Unable to create instructor");
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }

            return View(instructor);
        }

        // GET: Instructor/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            var instructor = await _db.Instructors.FindAsync(id);
            if (instructor == null)
            {
                return NotFound();
            }
            return View(instructor);
        }

        // POST: Instructor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("ID,LastName,FirstMidName,HireDate")] Instructor instructor)
        {
            if (ModelState.IsValid)
            {
                _db.Entry(instructor).State = EntityState.Modified;
                try
                {
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Unable to update instructor {InstructorID}", instructor.ID);
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }
            return View(instructor);
        }

        // GET: Instructor/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            var instructor = await _db.Instructors
                .Include(i => i.OfficeAssignment)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ID == id);
            if (instructor == null)
            {
                return NotFound();
            }
            return View(instructor);
        }

        // POST: Instructor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var instructor = await _db.Instructors
                .Include(i => i.OfficeAssignment)
                .FirstOrDefaultAsync(i => i.ID == id);
            if (instructor == null)
            {
                return NotFound();
            }

            // Departments administered by this instructor must not keep a dangling reference.
            var departments = await _db.Departments
                .Where(d => d.InstructorID == id)
                .ToListAsync();
            departments.ForEach(d => d.InstructorID = null);

            _db.Instructors.Remove(instructor);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
