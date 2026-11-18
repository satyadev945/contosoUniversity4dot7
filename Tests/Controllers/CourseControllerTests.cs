using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class CourseControllerTests : IDisposable
    {
        private readonly TestDb _db = new TestDb().Seed();

        public void Dispose() => _db.Dispose();

        private CourseController Create(SchoolContext ctx) =>
            new CourseController(ctx, NullLogger<CourseController>.Instance);

        [Fact]
        public async Task Index_ReturnsAllCourses()
        {
            using var ctx = _db.CreateContext();
            var result = await Create(ctx).Index();
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Course>>(view.Model);
            Assert.Equal(3, model.Count());
            Assert.All(model, c => Assert.NotNull(c.Department));
        }

        [Fact]
        public async Task Details_NullId_ReturnsBadRequest()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Details(null));
        }

        [Fact]
        public async Task Details_Unknown_ReturnsNotFound()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Details(99999));
        }

        [Fact]
        public async Task Details_Existing_ReturnsCourse()
        {
            using var ctx = _db.CreateContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Details(2021));
            var course = Assert.IsType<Course>(view.Model);
            Assert.Equal("Composition", course.Title);
            Assert.NotNull(course.Department);
        }

        [Fact]
        public async Task CreateGet_PopulatesDepartmentSelectList()
        {
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            var view = Assert.IsType<ViewResult>(await controller.Create());
            var list = Assert.IsType<SelectList>(controller.ViewData["DepartmentID"]);
            Assert.Equal(2, list.Count());
            Assert.Equal("English", list.First().Text);
        }

        [Fact]
        public async Task CreatePost_Valid_RedirectsAndPersists()
        {
            using (var ctx = _db.CreateContext())
            {
                var result = await Create(ctx).Create(new Course { CourseID = 5000, Title = "Physics", Credits = 3, DepartmentID = 1 });
                var redirect = Assert.IsType<RedirectToActionResult>(result);
                Assert.Equal("Index", redirect.ActionName);
            }
            using var verify = _db.CreateContext();
            Assert.NotNull(await verify.Courses.FindAsync(5000));
        }

        [Fact]
        public async Task CreatePost_InvalidModelState_ReturnsViewWithDepartments()
        {
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            controller.ModelState.AddModelError("Title", "Required");
            var course = new Course { CourseID = 5001, DepartmentID = 2 };
            var view = Assert.IsType<ViewResult>(await controller.Create(course));
            Assert.Same(course, view.Model);
            Assert.NotNull(controller.ViewData["DepartmentID"]);
        }

        [Fact]
        public async Task CreatePost_ForeignKeyViolation_AddsModelError()
        {
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            var view = Assert.IsType<ViewResult>(await controller.Create(new Course { CourseID = 5002, Title = "Bad", Credits = 1, DepartmentID = 12345 }));
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains(controller.ModelState[""].Errors, e => e.ErrorMessage.StartsWith("Unable to save changes"));
        }

        [Fact]
        public async Task EditGet_NullId_ReturnsBadRequest()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Edit((int?)null));
        }

        [Fact]
        public async Task EditGet_Unknown_ReturnsNotFound()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Edit((int?)99999));
        }

        [Fact]
        public async Task EditGet_Existing_ReturnsViewWithCourse()
        {
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            var view = Assert.IsType<ViewResult>(await controller.Edit((int?)1045));
            Assert.Equal(1045, Assert.IsType<Course>(view.Model).CourseID);
            Assert.IsType<SelectList>(controller.ViewData["DepartmentID"]);
        }

        [Fact]
        public async Task EditPost_Valid_UpdatesCourse()
        {
            using (var ctx = _db.CreateContext())
            {
                var result = await Create(ctx).Edit(new Course { CourseID = 1045, Title = "Calculus II", Credits = 5, DepartmentID = 2 });
                Assert.IsType<RedirectToActionResult>(result);
            }
            using var verify = _db.CreateContext();
            var c = await verify.Courses.FindAsync(1045);
            Assert.Equal("Calculus II", c.Title);
            Assert.Equal(5, c.Credits);
        }

        [Fact]
        public async Task EditPost_InvalidModelState_ReturnsView()
        {
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            controller.ModelState.AddModelError("Title", "bad");
            var course = new Course { CourseID = 1045, DepartmentID = 2 };
            var view = Assert.IsType<ViewResult>(await controller.Edit(course));
            Assert.Same(course, view.Model);
        }

        [Fact]
        public async Task EditPost_SaveFails_AddsModelError()
        {
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            var view = Assert.IsType<ViewResult>(await controller.Edit(new Course { CourseID = 1045, Title = "X1X", Credits = 1, DepartmentID = 98765 }));
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task DeleteGet_NullId_ReturnsBadRequest()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Delete(null));
        }

        [Fact]
        public async Task DeleteGet_Unknown_ReturnsNotFound()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete(99999));
        }

        [Fact]
        public async Task DeleteGet_Existing_ReturnsView()
        {
            using var ctx = _db.CreateContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Delete(3000));
            Assert.Equal("Unassigned", Assert.IsType<Course>(view.Model).Title);
        }

        [Fact]
        public async Task DeleteConfirmed_Existing_RemovesCourse()
        {
            using (var ctx = _db.CreateContext())
            {
                var result = await Create(ctx).DeleteConfirmed(2021);
                Assert.IsType<RedirectToActionResult>(result);
            }
            using var verify = _db.CreateContext();
            Assert.Null(await verify.Courses.FindAsync(2021));
            Assert.Empty(await verify.Enrollments.Where(e => e.CourseID == 2021).ToListAsync());
        }

        [Fact]
        public async Task DeleteConfirmed_Unknown_StillRedirects()
        {
            using var ctx = _db.CreateContext();
            var result = await Create(ctx).DeleteConfirmed(424242);
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(3, await ctx.Courses.CountAsync());
        }
    }
}
