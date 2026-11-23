using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Controllers
{
    public class InstructorController : Controller
    {
        private readonly SchoolContext _context;

        public InstructorController(SchoolContext context)
        {
            _context = context;
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
                var selectedInstructor = instructors.SingleOrDefault(i => i.ID == id.Value);
                if (selectedInstructor == null)
                {
                    return NotFound();
                }

                ViewData["InstructorID"] = id.Value;
                viewModel.Courses = selectedInstructor.Courses;

                if (courseID != null)
                {
                    var selectedCourse = selectedInstructor.Courses.SingleOrDefault(c => c.CourseID == courseID.Value);
                    if (selectedCourse == null)
                    {
                        return NotFound();
                    }

                    ViewData["CourseID"] = courseID.Value;

                    // Explicit loading
                    await _context.Entry(selectedCourse).Collection(c => c.Enrollments).LoadAsync();
                    foreach (var enrollment in selectedCourse.Enrollments)
                    {
                        await _context.Entry(enrollment).Reference(e => e.Student).LoadAsync();
                    }

                    viewModel.Enrollments = selectedCourse.Enrollments;
                }
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
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
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
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Instructors.AnyAsync(i => i.ID == instructor.ID))
                    {
                        return NotFound();
                    }
                    throw;
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
            if (instructor != null)
            {
                // Departments administered by this instructor must no longer reference them.
                var departments = await _context.Departments
                    .Where(d => d.InstructorID == id)
                    .ToListAsync();
                departments.ForEach(d => d.InstructorID = null);

                _context.Instructors.Remove(instructor);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
