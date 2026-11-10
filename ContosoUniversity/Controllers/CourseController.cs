using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Controllers
{
    public class CourseController : Controller
    {
        private readonly SchoolContext _db;

        public CourseController(SchoolContext db)
        {
            _db = db;
        }

        // GET: Course
        public async Task<IActionResult> Index()
        {
            var courses = await _db.Courses.Include(c => c.Department).AsNoTracking().ToListAsync();
            return View(courses);
        }

        // GET: Course/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Course course = await _db.Courses.Include(c => c.Department).AsNoTracking()
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
                _db.Courses.Add(course);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
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
            Course course = await _db.Courses.FindAsync(id);
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
                _db.Entry(course).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PopulateDepartmentsDropDownListAsync(course.DepartmentID);
            return View(course);
        }

        // GET: Course/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Course course = await _db.Courses.Include(c => c.Department).AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseID == id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // POST: Course/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Course course = await _db.Courses.FindAsync(id);
            if (course != null)
            {
                _db.Courses.Remove(course);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDepartmentsDropDownListAsync(object selectedDepartment = null)
        {
            var departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
            ViewBag.DepartmentList = new SelectList(departments, "DepartmentID", "Name", selectedDepartment);
        }
    }
}
