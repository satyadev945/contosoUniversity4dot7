using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class CourseControllerTests
    {
        private static CourseController Create(SchoolContext ctx) =>
            new CourseController(ctx, NullLogger<CourseController>.Instance);

        [Fact]
        public async Task Index_ReturnsAllCoursesWithDepartment()
        {
            var name = TestDb.CreateSeeded();
            using var ctx = TestDb.Create(name);

            var result = await Create(ctx).Index();

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Course>>(view.Model);
            Assert.Equal(2, model.Count);
            Assert.All(model, c => Assert.NotNull(c.Department));
        }

        [Fact]
        public async Task Details_NullId_ReturnsBadRequest()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            Assert.IsType<BadRequestResult>(await Create(ctx).Details(null));
        }

        [Fact]
        public async Task Details_UnknownId_ReturnsNotFound()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            Assert.IsType<NotFoundResult>(await Create(ctx).Details(999));
        }

        [Fact]
        public async Task Details_KnownId_ReturnsCourse()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var result = await Create(ctx).Details(1050);
            var course = Assert.IsType<Course>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal("Chemistry", course.Title);
            Assert.Equal("Engineering", course.Department.Name);
        }

        [Fact]
        public async Task CreateGet_PopulatesDepartmentsDropDown()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);

            var result = await controller.Create();

            Assert.IsType<ViewResult>(result);
            SelectList list = Assert.IsType<SelectList>((object)controller.ViewBag.DepartmentID);
            Assert.Equal(2, list.Count());
            Assert.Equal("Engineering", list.First().Text);
        }

        [Fact]
        public async Task CreatePost_ValidModel_PersistsAndRedirects()
        {
            var name = TestDb.CreateSeeded();
            using (var ctx = TestDb.Create(name))
            {
                var result = await Create(ctx).Create(new Course { CourseID = 3000, Title = "Physics", Credits = 4, DepartmentID = 1 });
                var redirect = Assert.IsType<RedirectToActionResult>(result);
                Assert.Equal("Index", redirect.ActionName);
            }
            using var verify = TestDb.Create(name);
            Assert.Equal("Physics", verify.Courses.Single(c => c.CourseID == 3000).Title);
        }

        [Fact]
        public async Task CreatePost_InvalidModelState_ReturnsViewWithCourse()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);
            controller.ModelState.AddModelError("Title", "Required");
            var course = new Course { CourseID = 3000 };

            var result = await controller.Create(course);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(course, view.Model);
            Assert.NotNull(controller.ViewBag.DepartmentID);
            Assert.Equal(0, ctx.Courses.Count(c => c.CourseID == 3000));
        }

        [Fact]
        public async Task CreatePost_DbUpdateException_AddsModelError()
        {
            using var ctx = TestDb.CreateThrowing(TestDb.CreateSeeded(), new DbUpdateException("boom"));
            var controller = Create(ctx);

            var result = await controller.Create(new Course { CourseID = 3000, Title = "Physics", DepartmentID = 1 });

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains(controller.ModelState[""].Errors, e => e.ErrorMessage.Contains("Unable to save changes"));
        }

        [Fact]
        public async Task EditGet_NullId_ReturnsBadRequest()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            Assert.IsType<BadRequestResult>(await Create(ctx).Edit((int?)null));
        }

        [Fact]
        public async Task EditGet_UnknownId_ReturnsNotFound()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            Assert.IsType<NotFoundResult>(await Create(ctx).Edit(999));
        }

        [Fact]
        public async Task EditGet_KnownId_ReturnsViewWithDropDown()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);

            var result = await controller.Edit(1050);

            var course = Assert.IsType<Course>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(1050, course.CourseID);
            SelectList list = Assert.IsType<SelectList>((object)controller.ViewBag.DepartmentID);
            Assert.Equal(1, (int)list.SelectedValue);
        }

        [Fact]
        public async Task EditPost_ValidModel_UpdatesAndRedirects()
        {
            var name = TestDb.CreateSeeded();
            using (var ctx = TestDb.Create(name))
            {
                var result = await Create(ctx).Edit(new Course { CourseID = 1050, Title = "Organic Chemistry", Credits = 4, DepartmentID = 2 });
                Assert.IsType<RedirectToActionResult>(result);
            }
            using var verify = TestDb.Create(name);
            var updated = verify.Courses.Single(c => c.CourseID == 1050);
            Assert.Equal("Organic Chemistry", updated.Title);
            Assert.Equal(4, updated.Credits);
            Assert.Equal(2, updated.DepartmentID);
        }

        [Fact]
        public async Task EditPost_InvalidModelState_ReturnsView()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);
            controller.ModelState.AddModelError("Title", "bad");

            var result = await controller.Edit(new Course { CourseID = 1050, DepartmentID = 1 });

            Assert.IsType<ViewResult>(result);
            Assert.NotNull(controller.ViewBag.DepartmentID);
        }

        [Fact]
        public async Task EditPost_DbUpdateException_AddsModelError()
        {
            using var ctx = TestDb.CreateThrowing(TestDb.CreateSeeded(), new DbUpdateException("boom"));
            var controller = Create(ctx);

            var result = await controller.Edit(new Course { CourseID = 1050, Title = "X Y Z", DepartmentID = 1 });

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState[""].Errors, e => e.ErrorMessage.Contains("Unable to save changes"));
        }

        [Fact]
        public async Task EditPost_ConcurrencyAndCourseDeleted_ReturnsNotFound()
        {
            using var ctx = TestDb.CreateThrowing(TestDb.CreateSeeded(), new DbUpdateConcurrencyException("gone"));

            var result = await Create(ctx).Edit(new Course { CourseID = 9999, Title = "Gone", DepartmentID = 1 });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditPost_ConcurrencyAndCourseExists_Rethrows()
        {
            using var ctx = TestDb.CreateThrowing(TestDb.CreateSeeded(), new DbUpdateConcurrencyException("conflict"));

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                Create(ctx).Edit(new Course { CourseID = 1050, Title = "Chem", DepartmentID = 1 }));
        }

        [Fact]
        public async Task DeleteGet_NullId_ReturnsBadRequest()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            Assert.IsType<BadRequestResult>(await Create(ctx).Delete(null));
        }

        [Fact]
        public async Task DeleteGet_UnknownId_ReturnsNotFound()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete(999));
        }

        [Fact]
        public async Task DeleteGet_KnownId_ReturnsCourse()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var result = await Create(ctx).Delete(2021);
            var course = Assert.IsType<Course>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal("Composition", course.Title);
        }

        [Fact]
        public async Task DeleteConfirmed_ExistingCourse_RemovesAndRedirects()
        {
            var name = TestDb.CreateSeeded();
            using (var ctx = TestDb.Create(name))
            {
                var result = await Create(ctx).DeleteConfirmed(2021);
                Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            }
            using var verify = TestDb.Create(name);
            Assert.Null(verify.Courses.Find(2021));
        }

        [Fact]
        public async Task DeleteConfirmed_MissingCourse_StillRedirects()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var result = await Create(ctx).DeleteConfirmed(12345);
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(2, ctx.Courses.Count());
        }
    }
}
