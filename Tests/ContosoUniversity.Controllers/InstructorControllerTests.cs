using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class InstructorControllerTests
    {
        private static InstructorController Create(TestDb db) =>
            new InstructorController(db.CreateContext(), TestDb.NullLog<InstructorController>()).WithContext();

        [Fact]
        public async Task Index_NoSelection_ReturnsOrderedInstructors()
        {
            using var db = new TestDb();
            db.Seed();
            var result = Assert.IsType<ViewResult>(await Create(db).Index(null, null));
            var vm = Assert.IsType<InstructorIndexData>(result.Model);
            Assert.Equal(new[] { "Abercrombie", "Fakhouri" }, vm.Instructors.Select(i => i.LastName).ToArray());
            Assert.Null(vm.Courses);
            Assert.Null(vm.Enrollments);
        }

        [Fact]
        public async Task Index_WithInstructor_ReturnsCourses()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            var result = Assert.IsType<ViewResult>(await c.Index(2, null));
            var vm = Assert.IsType<InstructorIndexData>(result.Model);
            Assert.Equal(2, vm.Courses!.Count());
            Assert.Equal(2, c.ViewData["InstructorID"]);
        }

        [Fact]
        public async Task Index_UnknownInstructor_NotFound()
        {
            using var db = new TestDb();
            db.Seed();
            Assert.IsType<NotFoundResult>(await Create(db).Index(99, null));
        }

        [Fact]
        public async Task Index_WithInstructorAndCourse_LoadsEnrollments()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            var result = Assert.IsType<ViewResult>(await c.Index(1, 1050));
            var vm = Assert.IsType<InstructorIndexData>(result.Model);
            Assert.Equal(3, vm.Enrollments!.Count());
            Assert.All(vm.Enrollments!, e => Assert.NotNull(e.Student));
            Assert.Equal(1050, c.ViewData["CourseID"]);
        }

        [Fact]
        public async Task Index_UnknownCourse_NotFound()
        {
            using var db = new TestDb();
            db.Seed();
            Assert.IsType<NotFoundResult>(await Create(db).Index(1, 2021));
        }

        [Fact]
        public async Task Index_CourseWithoutInstructor_IgnoresCourse()
        {
            using var db = new TestDb();
            db.Seed();
            var result = Assert.IsType<ViewResult>(await Create(db).Index(null, 1050));
            Assert.Null(Assert.IsType<InstructorIndexData>(result.Model).Enrollments);
        }

        [Fact]
        public async Task Details_NullId_BadRequest()
        {
            using var db = new TestDb();
            Assert.IsType<BadRequestResult>(await Create(db).Details(null));
        }

        [Fact]
        public async Task Details_Missing_NotFound()
        {
            using var db = new TestDb();
            Assert.IsType<NotFoundResult>(await Create(db).Details(5));
        }

        [Fact]
        public async Task Details_Existing_ReturnsInstructorWithOffice()
        {
            using var db = new TestDb();
            db.Seed();
            var result = Assert.IsType<ViewResult>(await Create(db).Details(1));
            var i = Assert.IsType<Instructor>(result.Model);
            Assert.Equal("Smith 17", i.OfficeAssignment!.Location);
        }

        [Fact]
        public void CreateGet_ReturnsView()
        {
            using var db = new TestDb();
            Assert.IsType<ViewResult>(Create(db).Create());
        }

        [Fact]
        public async Task CreatePost_Valid_Persists()
        {
            using var db = new TestDb();
            var inst = new Instructor { LastName = "Zheng", FirstMidName = "Roger", HireDate = new DateTime(2004, 2, 12) };
            Assert.IsType<RedirectToActionResult>(await Create(db).Create(inst));
            using var ctx = db.CreateContext();
            Assert.Equal(1, await ctx.Instructors.CountAsync());
        }

        [Fact]
        public async Task CreatePost_InvalidModel_ReturnsView()
        {
            using var db = new TestDb();
            var c = Create(db);
            c.ModelState.AddModelError("LastName", "required");
            Assert.IsType<ViewResult>(await c.Create(new Instructor()));
        }

        [Fact]
        public async Task CreatePost_DbUpdateException_AddsModelError()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            // duplicate primary key
            var result = await c.Create(new Instructor { ID = 1, LastName = "Dup", FirstMidName = "X" });
            Assert.IsType<ViewResult>(result);
            Assert.False(c.ModelState.IsValid);
        }

        [Fact]
        public async Task EditGet_NullId_BadRequest()
        {
            using var db = new TestDb();
            Assert.IsType<BadRequestResult>(await Create(db).Edit((int?)null));
        }

        [Fact]
        public async Task EditGet_Missing_NotFound()
        {
            using var db = new TestDb();
            Assert.IsType<NotFoundResult>(await Create(db).Edit(77));
        }

        [Fact]
        public async Task EditGet_Existing_ReturnsView()
        {
            using var db = new TestDb();
            db.Seed();
            var result = Assert.IsType<ViewResult>(await Create(db).Edit(2));
            Assert.Equal("Fakhouri", Assert.IsType<Instructor>(result.Model).LastName);
        }

        [Fact]
        public async Task EditPost_Valid_Updates()
        {
            using var db = new TestDb();
            db.Seed();
            var updated = new Instructor { ID = 2, LastName = "Renamed", FirstMidName = "Fadi", HireDate = new DateTime(2002, 7, 6) };
            Assert.IsType<RedirectToActionResult>(await Create(db).Edit(updated));
            using var ctx = db.CreateContext();
            Assert.Equal("Renamed", (await ctx.Instructors.FindAsync(2))!.LastName);
        }

        [Fact]
        public async Task EditPost_InvalidModel_ReturnsView()
        {
            using var db = new TestDb();
            var c = Create(db);
            c.ModelState.AddModelError("x", "y");
            Assert.IsType<ViewResult>(await c.Edit(new Instructor { ID = 1 }));
        }

        [Fact]
        public async Task EditPost_NonexistentInstructor_AddsModelError()
        {
            using var db = new TestDb();
            var c = Create(db);
            var result = await c.Edit(new Instructor { ID = 500, LastName = "Ghost", FirstMidName = "G" });
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task DeleteGet_NullId_BadRequest()
        {
            using var db = new TestDb();
            Assert.IsType<BadRequestResult>(await Create(db).Delete(null));
        }

        [Fact]
        public async Task DeleteGet_Missing_NotFound()
        {
            using var db = new TestDb();
            Assert.IsType<NotFoundResult>(await Create(db).Delete(3));
        }

        [Fact]
        public async Task DeleteGet_Existing_ReturnsView()
        {
            using var db = new TestDb();
            db.Seed();
            Assert.IsType<ViewResult>(await Create(db).Delete(1));
        }

        [Fact]
        public async Task DeleteConfirmed_Missing_NotFound()
        {
            using var db = new TestDb();
            Assert.IsType<NotFoundResult>(await Create(db).DeleteConfirmed(3));
        }

        [Fact]
        public async Task DeleteConfirmed_Existing_ClearsDepartmentAdministrator()
        {
            using var db = new TestDb();
            db.Seed();
            Assert.IsType<RedirectToActionResult>(await Create(db).DeleteConfirmed(1));
            using var ctx = db.CreateContext();
            Assert.Null(await ctx.Instructors.FindAsync(1));
            Assert.Null((await ctx.Departments.FindAsync(1))!.InstructorID);
            Assert.Empty(ctx.OfficeAssignments);
        }
    }
}
