using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Controllers
{
    public class InstructorController : Controller
    {
        private readonly SchoolContext _db;

        public InstructorController(SchoolContext db)
        {
            _db = db;
        }

        // GET: Instructor
        public async Task<IActionResult> Index(int? id, int? courseID)
        {
            var viewModel = new InstructorIndexData();
            viewModel.Instructors = await _db.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.Courses)
                    .ThenInclude(c => c.Department)
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

                viewModel.Enrollments = await _db.Enrollments
                    .Include(e => e.Student)
                    .Where(e => e.CourseID == selectedCourse.CourseID)
                    .AsNoTracking()
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
            Instructor instructor = await _db.Instructors.Include(i => i.OfficeAssignment).AsNoTracking()
                .FirstOrDefaultAsync(i => i.ID == id);
            if (instructor == null)
            {
                return NotFound();
            }
            return View(instructor);
        }

        // GET: Instructor/Create
        public async Task<IActionResult> Create()
        {
            await PopulateOfficeAssignmentsAsync();
            return View();
        }

        // POST: Instructor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LastName,FirstMidName,HireDate")] Instructor instructor)
        {
            if (ModelState.IsValid)
            {
                _db.Instructors.Add(instructor);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateOfficeAssignmentsAsync(instructor.ID);
            return View(instructor);
        }

        // GET: Instructor/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Instructor instructor = await _db.Instructors.FindAsync(id);
            if (instructor == null)
            {
                return NotFound();
            }
            await PopulateOfficeAssignmentsAsync(instructor.ID);
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
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PopulateOfficeAssignmentsAsync(instructor.ID);
            return View(instructor);
        }

        // GET: Instructor/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Instructor instructor = await _db.Instructors.Include(i => i.OfficeAssignment).AsNoTracking()
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
            Instructor instructor = await _db.Instructors.FindAsync(id);
            if (instructor != null)
            {
                _db.Instructors.Remove(instructor);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateOfficeAssignmentsAsync(object selectedId = null)
        {
            var offices = await _db.OfficeAssignments.AsNoTracking().ToListAsync();
            ViewBag.ID = new SelectList(offices, "InstructorID", "Location", selectedId);
        }
    }
}
