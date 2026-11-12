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
using ContosoUniversity.Security;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;

namespace ContosoUniversity.Controllers
{
    public class InstructorController : Controller
    {
        private SchoolContext db = new SchoolContext();

        // GET: Instructor
        public async Task<ActionResult> Index(int? id, int? courseID)
        {
            var viewModel = new InstructorIndexData();
            // Materialize asynchronously so the view never triggers blocking data access
            var instructors = await db.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.Courses.Select(c => c.Department))
                .OrderBy(i => i.LastName)
                .ToListAsync();
            viewModel.Instructors = instructors;

            if (id != null)
            {
                // Numeric value only (int?) - rendered via Razor auto-encoding, no raw HTML involved
                ViewBag.InstructorID = id.Value;
                viewModel.Courses = viewModel.Instructors.Where(
                    i => i.ID == id.Value).Single().Courses;
            }

            if (courseID != null)
            {
                // Numeric value only (int?) - rendered via Razor auto-encoding, no raw HTML involved
                ViewBag.CourseID = courseID.Value;
                // Lazy loading
                //viewModel.Enrollments = viewModel.Courses.Where(
                //    x => x.CourseID == courseID).Single().Enrollments;
                // Explicit loading
                var selectedCourse = viewModel.Courses.Where(x => x.CourseID == courseID).Single();
                await db.Entry(selectedCourse).Collection(x => x.Enrollments).LoadAsync();
                foreach (Enrollment enrollment in selectedCourse.Enrollments)
                {
                    await db.Entry(enrollment).Reference(x => x.Student).LoadAsync();
                }

                viewModel.Enrollments = selectedCourse.Enrollments.ToList();
            }

            return View(viewModel);
        }

        // GET: Instructor/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Instructor instructor = await db.Instructors.FindAsync(id);
            if (instructor == null)
            {
                return HttpNotFound();
            }
            return View(instructor);
        }

        // GET: Instructor/Create
        public async Task<ActionResult> Create()
        {
            ViewBag.ID = SafeSelectList.Create(await db.OfficeAssignments.ToListAsync(), o => o.InstructorID, o => o.Location);
            return View();
        }

        // POST: Instructor/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "ID,LastName,FirstMidName,HireDate")] Instructor instructor)
        {
            if (ModelState.IsValid)
            {
                db.Instructors.Add(instructor);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.ID = SafeSelectList.Create(await db.OfficeAssignments.ToListAsync(), o => o.InstructorID, o => o.Location, instructor.ID);
            return View(instructor);
        }

        // GET: Instructor/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Instructor instructor = await db.Instructors.FindAsync(id);
            if (instructor == null)
            {
                return HttpNotFound();
            }
            ViewBag.ID = SafeSelectList.Create(await db.OfficeAssignments.ToListAsync(), o => o.InstructorID, o => o.Location, instructor.ID);
            return View(instructor);
        }

        // POST: Instructor/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "ID,LastName,FirstMidName,HireDate")] Instructor instructor)
        {
            if (ModelState.IsValid)
            {
                db.Entry(instructor).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.ID = SafeSelectList.Create(await db.OfficeAssignments.ToListAsync(), o => o.InstructorID, o => o.Location, instructor.ID);
            return View(instructor);
        }

        // GET: Instructor/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Instructor instructor = await db.Instructors.FindAsync(id);
            if (instructor == null)
            {
                return HttpNotFound();
            }
            return View(instructor);
        }

        // POST: Instructor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Instructor instructor = await db.Instructors.FindAsync(id);
            db.Instructors.Remove(instructor);
            await db.SaveChangesAsync();
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
