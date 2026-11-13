using ContosoUniversity.Controllers;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Tests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class CourseControllerTests
    {
        private static async Task<SchoolContext> SeededContext()
        {
            var ctx = TestHelpers.CreateContext();
            ctx.Departments.AddRange(
                new Department { DepartmentID = 1, Name = "Mathematics", Budget = 1, StartDate = DateTime.Today },
                new Department { DepartmentID = 2, Name = "English", Budget = 1, StartDate = DateTime.Today });
            ctx.Courses.AddRange(
                new Course { CourseID = 1045, Title = "Calculus", Credits = 4, DepartmentID = 1 },
                new Course { CourseID = 2021, Title = "Composition", Credits = 3, DepartmentID = 2 });
            await ctx.SaveChangesAsync();
            return ctx;
        }

        private static CourseController Create(SchoolContext ctx) =>
            new CourseController(ctx, NullLogger<CourseController>.Instance);

        [Fact]
        public async Task Index_ReturnsAllCoursesWithDepartments()
        {
            using var ctx = await SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Index());
            var model = Assert.IsAssignableFrom<IEnumerable<Course>>(view.Model).ToList();
            Assert.Equal(2, model.Count);
            Assert.All(model, c => Assert.NotNull(c.Department));
        }

        [Fact]
        public async Task Details_NullId_ReturnsBadRequest()
        {
            using var ctx = await SeededContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Details(null));
        }

        [Fact]
        public async Task Details_UnknownId_ReturnsNotFound()
        {
            using var ctx = await SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Details(9999));
        }

        [Fact]
        public async Task Details_ExistingId_ReturnsCourse()
        {
            using var ctx = await SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Details(1045));
            var course = Assert.IsType<Course>(view.Model);
            Assert.Equal("Calculus", course.Title);
            Assert.Equal("Mathematics", course.Department.Name);
        }

        [Fact]
        public async Task CreateGet_PopulatesDepartmentsSortedByName()
        {
            using var ctx = await SeededContext();
            var c = Create(ctx);
            Assert.IsType<ViewResult>(await c.Create());
            SelectList list = c.ViewBag.DepartmentID;
            var items = list.ToList();
            Assert.Equal(2, items.Count);
            Assert.Equal("English", items[0].Text);
        }

        [Fact]
        public async Task CreatePost_ValidModel_AddsCourseAndRedirects()
        {
            using var ctx = await SeededContext();
            var result = await Create(ctx).Create(new Course { CourseID = 3141, Title = "Trigonometry", Credits = 4, DepartmentID = 1 });
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal(3, await ctx.Courses.CountAsync());
        }

        [Fact]
        public async Task CreatePost_InvalidModel_ReturnsViewWithCourse()
        {
            using var ctx = await SeededContext();
            var c = Create(ctx);
            c.ModelState.AddModelError("Title", "Required");
            var course = new Course { CourseID = 5, DepartmentID = 2 };
            var view = Assert.IsType<ViewResult>(await c.Create(course));
            Assert.Same(course, view.Model);
            Assert.NotNull(c.ViewBag.DepartmentID);
            Assert.Equal(2, await ctx.Courses.CountAsync());
        }

        [Fact]
        public async Task CreatePost_SaveFails_AddsModelError()
        {
            using var ctx = TestHelpers.CreateThrowingContext();
            var c = new CourseController(ctx, NullLogger<CourseController>.Instance);
            var view = Assert.IsType<ViewResult>(await c.Create(new Course { CourseID = 1, Title = "Abc", Credits = 1, DepartmentID = 1 }));
            Assert.False(c.ModelState.IsValid);
            Assert.NotNull(view.Model);
        }

        [Fact]
        public async Task EditGet_NullId_ReturnsBadRequest()
        {
            using var ctx = await SeededContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Edit((int?)null));
        }

        [Fact]
        public async Task EditGet_UnknownId_ReturnsNotFound()
        {
            using var ctx = await SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Edit(1));
        }

        [Fact]
        public async Task EditGet_ExistingId_ReturnsCourseAndDepartments()
        {
            using var ctx = await SeededContext();
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Edit(1045));
            Assert.Equal(1045, Assert.IsType<Course>(view.Model).CourseID);
            Assert.NotNull(c.ViewBag.DepartmentID);
        }

        [Fact]
        public async Task EditPost_ValidModel_UpdatesAndRedirects()
        {
            using var ctx = await SeededContext();
            var edited = new Course { CourseID = 1045, Title = "Calculus II", Credits = 5, DepartmentID = 1 };
            // Detach tracked entities so Entry(...).State can attach the edited one
            ctx.ChangeTracker.Clear();
            var result = await Create(ctx).Edit(edited);
            Assert.IsType<RedirectToActionResult>(result);
            ctx.ChangeTracker.Clear();
            Assert.Equal("Calculus II", (await ctx.Courses.FindAsync(1045)).Title);
        }

        [Fact]
        public async Task EditPost_InvalidModel_ReturnsView()
        {
            using var ctx = await SeededContext();
            var c = Create(ctx);
            c.ModelState.AddModelError("Title", "bad");
            var course = new Course { CourseID = 1045, DepartmentID = 1 };
            var view = Assert.IsType<ViewResult>(await c.Edit(course));
            Assert.Same(course, view.Model);
        }

        [Fact]
        public async Task EditPost_ConcurrencyFailure_AddsModelError()
        {
            using var ctx = await SeededContext();
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Edit(new Course { CourseID = 7777, Title = "Nope", Credits = 1, DepartmentID = 1 }));
            Assert.False(c.ModelState.IsValid);
            Assert.NotNull(view.Model);
        }

        [Fact]
        public async Task DeleteGet_NullId_ReturnsBadRequest()
        {
            using var ctx = await SeededContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Delete(null));
        }

        [Fact]
        public async Task DeleteGet_UnknownId_ReturnsNotFound()
        {
            using var ctx = await SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete(1));
        }

        [Fact]
        public async Task DeleteGet_ExistingId_ReturnsView()
        {
            using var ctx = await SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Delete(2021));
            Assert.Equal("Composition", Assert.IsType<Course>(view.Model).Title);
        }

        [Fact]
        public async Task DeleteConfirmed_UnknownId_ReturnsNotFound()
        {
            using var ctx = await SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).DeleteConfirmed(1));
        }

        [Fact]
        public async Task DeleteConfirmed_ExistingId_RemovesAndRedirects()
        {
            using var ctx = await SeededContext();
            var result = await Create(ctx).DeleteConfirmed(2021);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Equal(1, await ctx.Courses.CountAsync());
        }
    }
}
