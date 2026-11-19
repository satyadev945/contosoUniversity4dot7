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
        private readonly SchoolContext _db;

        public InstructorController(SchoolContext db)
        {
            _db = db;
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

            if (courseID != null)
            {
                ViewBag.CourseID = courseID.Value;
                var selectedCourse = viewModel.Courses?.SingleOrDefault(x => x.CourseID == courseID);
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
            Instructor instructor = await _db.Instructors
                .Include(i => i.OfficeAssignment)
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
        public async Task<IActionResult> Create([Bind("ID,LastName,FirstMidName,HireDate")] Instructor instructor)
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
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            // Load then update only the whitelisted properties to avoid overposting and nulling unbound columns.
            var instructorToUpdate = await _db.Instructors.FirstOrDefaultAsync(i => i.ID == id);
            if (instructorToUpdate == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync<Instructor>(
                instructorToUpdate,
                "",
                i => i.LastName, i => i.FirstMidName, i => i.HireDate))
            {
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateOfficeAssignmentsAsync(instructorToUpdate.ID);
            return View(instructorToUpdate);
        }

        // GET: Instructor/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Instructor instructor = await _db.Instructors
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
            Instructor instructor = await _db.Instructors
                .Include(i => i.OfficeAssignment)
                .FirstOrDefaultAsync(i => i.ID == id);
            if (instructor != null)
            {
                // Departments administered by this instructor must not keep a dangling reference.
                var departments = await _db.Departments.Where(d => d.InstructorID == id).ToListAsync();
                departments.ForEach(d => d.InstructorID = null);

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
