using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class CourseControllerTests
    {
        private static CourseController Create(TestDb db) =>
            new CourseController(db.CreateContext(), TestDb.NullLog<CourseController>()).WithContext();

        [Fact]
        public async Task Index_ReturnsAllCourses()
        {
            using var db = new TestDb();
            db.Seed();
            var result = Assert.IsType<ViewResult>(await Create(db).Index());
            var model = Assert.IsAssignableFrom<IEnumerable<Course>>(result.Model).ToList();
            Assert.Equal(2, model.Count);
            Assert.All(model, c => Assert.NotNull(c.Department));
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
            db.Seed();
            Assert.IsType<NotFoundResult>(await Create(db).Details(9999));
        }

        [Fact]
        public async Task Details_Existing_ReturnsCourse()
        {
            using var db = new TestDb();
            db.Seed();
            var result = Assert.IsType<ViewResult>(await Create(db).Details(1050));
            Assert.Equal("Chemistry", Assert.IsType<Course>(result.Model).Title);
        }

        [Fact]
        public async Task CreateGet_PopulatesDepartments()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            Assert.IsType<ViewResult>(await c.Create());
            var list = Assert.IsType<SelectList>(c.ViewData["DepartmentID"]);
            Assert.Equal(2, list.Count());
            Assert.Equal("English", list.First().Text);
        }

        [Fact]
        public async Task CreatePost_Valid_RedirectsAndPersists()
        {
            using var db = new TestDb();
            db.Seed();
            var result = await Create(db).Create(new Course { CourseID = 3000, Title = "Physics", Credits = 3, DepartmentID = 1 });
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            using var ctx = db.CreateContext();
            Assert.NotNull(await ctx.Courses.FindAsync(3000));
        }

        [Fact]
        public async Task CreatePost_InvalidModelState_ReturnsView()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            c.ModelState.AddModelError("Title", "bad");
            var result = Assert.IsType<ViewResult>(await c.Create(new Course { CourseID = 3001, DepartmentID = 1 }));
            Assert.NotNull(result.Model);
            Assert.NotNull(c.ViewData["DepartmentID"]);
        }

        [Fact]
        public async Task CreatePost_DbUpdateException_AddsModelError()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            // Department 999 does not exist => FK violation
            var result = await c.Create(new Course { CourseID = 3002, Title = "Bad", Credits = 1, DepartmentID = 999 });
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
            db.Seed();
            Assert.IsType<NotFoundResult>(await Create(db).Edit(12345));
        }

        [Fact]
        public async Task EditGet_Existing_ReturnsViewWithDepartments()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            var result = Assert.IsType<ViewResult>(await c.Edit(1050));
            Assert.Equal(1050, Assert.IsType<Course>(result.Model).CourseID);
            Assert.NotNull(c.ViewData["DepartmentID"]);
        }

        [Fact]
        public async Task EditPost_Valid_UpdatesAndRedirects()
        {
            using var db = new TestDb();
            db.Seed();
            var result = await Create(db).Edit(new Course { CourseID = 1050, Title = "Organic Chem", Credits = 5, DepartmentID = 2 });
            Assert.IsType<RedirectToActionResult>(result);
            using var ctx = db.CreateContext();
            var c = await ctx.Courses.FindAsync(1050);
            Assert.Equal("Organic Chem", c!.Title);
            Assert.Equal(5, c.Credits);
            Assert.Equal(2, c.DepartmentID);
        }

        [Fact]
        public async Task EditPost_InvalidModel_ReturnsView()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            c.ModelState.AddModelError("Credits", "bad");
            Assert.IsType<ViewResult>(await c.Edit(new Course { CourseID = 1050, Title = "X", Credits = 99, DepartmentID = 1 }));
        }

        [Fact]
        public async Task EditPost_DbUpdateException_AddsModelError()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            var result = await c.Edit(new Course { CourseID = 1050, Title = "Chem", Credits = 3, DepartmentID = 999 });
            Assert.IsType<ViewResult>(result);
            Assert.False(c.ModelState.IsValid);
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
            Assert.IsType<NotFoundResult>(await Create(db).Delete(1));
        }

        [Fact]
        public async Task DeleteGet_Existing_ReturnsView()
        {
            using var db = new TestDb();
            db.Seed();
            var result = Assert.IsType<ViewResult>(await Create(db).Delete(2021));
            Assert.Equal("Composition", Assert.IsType<Course>(result.Model).Title);
        }

        [Fact]
        public async Task DeleteConfirmed_Missing_NotFound()
        {
            using var db = new TestDb();
            Assert.IsType<NotFoundResult>(await Create(db).DeleteConfirmed(1));
        }

        [Fact]
        public async Task DeleteConfirmed_Existing_Removes()
        {
            using var db = new TestDb();
            db.Seed();
            Assert.IsType<RedirectToActionResult>(await Create(db).DeleteConfirmed(2021));
            using var ctx = db.CreateContext();
            Assert.Null(await ctx.Courses.FindAsync(2021));
            Assert.NotNull(await ctx.Courses.FindAsync(1050));
        }
    }
}
