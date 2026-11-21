using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            var instructors = await _context.Instructors
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
                viewModel.Courses = selectedInstructor.Courses ?? new List<Course>();
            }

            if (courseID != null && viewModel.Courses != null)
            {
                ViewBag.CourseID = courseID.Value;
                var selectedCourse = viewModel.Courses.SingleOrDefault(x => x.CourseID == courseID);
                if (selectedCourse == null)
                {
                    return NotFound();
                }

                // Eager loading of enrollments and students (replaces explicit N+1 loading).
                viewModel.Enrollments = await _context.Enrollments
                    .Include(e => e.Student)
                    .Where(e => e.CourseID == courseID.Value)
                    .ToListAsync();
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
            Instructor instructor = await _context.Instructors
                .Include(i => i.OfficeAssignment)
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
            ViewBag.ID = new SelectList(_context.OfficeAssignments, "InstructorID", "Location");
            return View();
        }

        // POST: Instructor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,LastName,FirstMidName,HireDate")] Instructor instructor)
        {
            if (ModelState.IsValid)
            {
                _context.Instructors.Add(instructor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ID = new SelectList(_context.OfficeAssignments, "InstructorID", "Location", instructor.ID);
            return View(instructor);
        }

        // GET: Instructor/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Instructor instructor = await _context.Instructors.FindAsync(id);
            if (instructor == null)
            {
                return NotFound();
            }
            ViewBag.ID = new SelectList(_context.OfficeAssignments, "InstructorID", "Location", instructor.ID);
            return View(instructor);
        }

        // POST: Instructor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("ID,LastName,FirstMidName,HireDate")] Instructor instructor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Entry(instructor).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Concurrency error while updating instructor {InstructorID}", instructor.ID);
                    if (!await _context.Instructors.AnyAsync(i => i.ID == instructor.ID))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            ViewBag.ID = new SelectList(_context.OfficeAssignments, "InstructorID", "Location", instructor.ID);
            return View(instructor);
        }

        // GET: Instructor/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Instructor instructor = await _context.Instructors
                .Include(i => i.OfficeAssignment)
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
            Instructor instructor = await _context.Instructors.FindAsync(id);
            if (instructor != null)
            {
                _context.Instructors.Remove(instructor);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
