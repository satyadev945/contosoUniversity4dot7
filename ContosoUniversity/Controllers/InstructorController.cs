using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Controllers
{
    public class InstructorController : Controller
    {
        private readonly SchoolContext _context;
        private readonly ILogger<InstructorController> _logger;

        public InstructorController(SchoolContext context, ILogger<InstructorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Instructor
        public async Task<IActionResult> Index(int? id, int? courseID)
        {
            var viewModel = new InstructorIndexData();
            viewModel.Instructors = await _context.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.Courses!)
                    .ThenInclude(c => c.Department)
                .Include(i => i.Courses!)
                    .ThenInclude(c => c.Enrollments!)
                        .ThenInclude(e => e.Student)
                .AsSplitQuery()
                .AsNoTracking()
                .OrderBy(i => i.LastName)
                .ToListAsync();

            if (id != null)
            {
                ViewBag.InstructorID = id.Value;
                var selectedInstructor = viewModel.Instructors.SingleOrDefault(i => i.ID == id.Value);
                if (selectedInstructor == null)
                {
                    return NotFound();
                }
                viewModel.Courses = selectedInstructor.Courses ?? new List<Course>();
            }

            if (courseID != null)
            {
                ViewBag.CourseID = courseID.Value;
                var selectedCourse = viewModel.Courses?.SingleOrDefault(x => x.CourseID == courseID);
                if (selectedCourse == null)
                {
                    return NotFound();
                }
                viewModel.Enrollments = selectedCourse.Enrollments ?? new List<Enrollment>();
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
            var instructor = await _context.Instructors
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
        public async Task<IActionResult> Create([Bind("LastName,FirstMidName,HireDate")] Instructor instructor)
        {
            if (ModelState.IsValid)
            {
                _context.Instructors.Add(instructor);
                try
                {
                    await _context.SaveChangesAsync();
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
            var instructor = await _context.Instructors.FindAsync(id);
            if (instructor == null)
            {
                return NotFound();
            }
            return View(instructor);
        }

        // POST: Instructor/Edit/5
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? ID)
        {
            if (ID == null)
            {
                return BadRequest();
            }

            var instructorToUpdate = await _context.Instructors.FirstOrDefaultAsync(i => i.ID == ID);
            if (instructorToUpdate == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync(instructorToUpdate, "",
                    i => i.LastName, i => i.FirstMidName, i => i.HireDate))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Unable to update instructor {InstructorID}", ID);
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }
            return View(instructorToUpdate);
        }

        // GET: Instructor/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            var instructor = await _context.Instructors
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
            var instructor = await _context.Instructors.FindAsync(id);
            if (instructor == null)
            {
                return NotFound();
            }

            // Department.InstructorID is a nullable FK: clear it before removing the administrator.
            var departments = await _context.Departments
                .Where(d => d.InstructorID == id)
                .ToListAsync();
            departments.ForEach(d => d.InstructorID = null);

            _context.Instructors.Remove(instructor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
