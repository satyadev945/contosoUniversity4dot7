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
    public class CourseControllerTests
    {
        private static CourseController Create(SchoolContext ctx)
        {
            var c = new CourseController(ctx, NullLogger<CourseController>.Instance);
            ControllerTestHelper.Setup(c);
            return c;
        }

        [Fact]
        public async Task Index_ReturnsAllCoursesWithDepartment()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Index();
            var model = ControllerTestHelper.ModelOf<List<Course>>(result);
            Assert.Equal(3, model.Count);
            Assert.All(model, c => Assert.NotNull(c.Department));
        }

        [Fact]
        public async Task Details_NullId_ReturnsBadRequest()
        {
            using var ctx = TestDb.CreateInMemory();
            Assert.IsType<BadRequestResult>(await Create(ctx).Details(null));
        }

        [Fact]
        public async Task Details_Unknown_ReturnsNotFound()
        {
            using var ctx = TestDb.CreateInMemory();
            Assert.IsType<NotFoundResult>(await Create(ctx).Details(999));
        }

        [Fact]
        public async Task Details_Existing_ReturnsCourse()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Details(1045);
            var course = ControllerTestHelper.ModelOf<Course>(result);
            Assert.Equal("Calculus", course.Title);
            Assert.NotNull(course.Department);
        }

        [Fact]
        public async Task Create_Get_PopulatesDepartmentsOrderedByName()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var controller = Create(ctx);
            var result = await controller.Create();
            Assert.IsType<ViewResult>(result);
            var list = Assert.IsType<SelectList>(controller.ViewBag.DepartmentID);
            Assert.Equal(new[] { "English", "Mathematics" }, ((IEnumerable<SelectListItem>)list).Select(i => i.Text).ToArray());
        }

        [Fact]
        public async Task Create_Post_Valid_AddsAndRedirects()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Create(new Course { CourseID = 5000, Title = "Physics", Credits = 3, DepartmentID = 1 });
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            using var verify = TestDb.CreateInMemory(db);
            Assert.NotNull(await verify.Courses.FindAsync(5000));
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsViewWithDepartments()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var controller = Create(ctx);
            controller.ModelState.AddModelError("Title", "bad");
            var course = new Course { CourseID = 5001, DepartmentID = 2 };
            var result = await controller.Create(course);
            Assert.Same(course, ControllerTestHelper.ModelOf<Course>(result));
            Assert.NotNull(controller.ViewBag.DepartmentID);
            using var verify = TestDb.CreateInMemory(db);
            Assert.Null(await verify.Courses.FindAsync(5001));
        }

        [Fact]
        public async Task Edit_Get_NullId_ReturnsBadRequest()
        {
            using var ctx = TestDb.CreateInMemory();
            Assert.IsType<BadRequestResult>(await Create(ctx).Edit((int?)null));
        }

        [Fact]
        public async Task Edit_Get_Unknown_ReturnsNotFound()
        {
            using var ctx = TestDb.CreateInMemory();
            Assert.IsType<NotFoundResult>(await Create(ctx).Edit((int?)42));
        }

        [Fact]
        public async Task Edit_Get_Existing_ReturnsViewWithDepartments()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var controller = Create(ctx);
            var result = await controller.Edit((int?)2021);
            Assert.Equal(2021, ControllerTestHelper.ModelOf<Course>(result).CourseID);
            var list = Assert.IsType<SelectList>(controller.ViewBag.DepartmentID);
            Assert.Equal(1, list.SelectedValue);
        }

        [Fact]
        public async Task Edit_Post_Valid_UpdatesAndRedirects()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Edit(new Course { CourseID = 2021, Title = "Composition II", Credits = 2, DepartmentID = 1 });
            Assert.IsType<RedirectToActionResult>(result);
            using var verify = TestDb.CreateInMemory(db);
            var updated = await verify.Courses.FindAsync(2021);
            Assert.Equal("Composition II", updated.Title);
            Assert.Equal(2, updated.Credits);
        }

        [Fact]
        public async Task Edit_Post_InvalidModel_ReturnsView()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var controller = Create(ctx);
            controller.ModelState.AddModelError("Title", "bad");
            var result = await controller.Edit(new Course { CourseID = 2021, DepartmentID = 1 });
            Assert.IsType<ViewResult>(result);
            Assert.NotNull(controller.ViewBag.DepartmentID);
        }

        [Fact]
        public async Task Edit_Post_ConcurrencyAndCourseMissing_ReturnsNotFound()
        {
            // The in-memory provider throws DbUpdateConcurrencyException when updating a non-existent row.
            using var ctx = TestDb.CreateInMemory();
            var result = await Create(ctx).Edit(new Course { CourseID = 12345, Title = "Ghost", Credits = 1, DepartmentID = 1 });
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ConcurrencyAndCourseExists_Rethrows()
        {
            var db = TestDb.SeedBasic();
            var options = TestDb.InMemoryOptions(db);
            using var ctx = new ThrowingSchoolContext(options, new DbUpdateConcurrencyException("conflict"));
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                Create(ctx).Edit(new Course { CourseID = 2021, Title = "Composition", Credits = 3, DepartmentID = 1 }));
        }

        [Fact]
        public async Task Delete_Get_NullId_ReturnsBadRequest()
        {
            using var ctx = TestDb.CreateInMemory();
            Assert.IsType<BadRequestResult>(await Create(ctx).Delete(null));
        }

        [Fact]
        public async Task Delete_Get_Unknown_ReturnsNotFound()
        {
            using var ctx = TestDb.CreateInMemory();
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete(7));
        }

        [Fact]
        public async Task Delete_Get_Existing_ReturnsCourse()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Delete(2042);
            Assert.Equal("Literature", ControllerTestHelper.ModelOf<Course>(result).Title);
        }

        [Fact]
        public async Task DeleteConfirmed_Existing_RemovesAndRedirects()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).DeleteConfirmed(2042);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            using var verify = TestDb.CreateInMemory(db);
            Assert.Null(await verify.Courses.FindAsync(2042));
        }

        [Fact]
        public async Task DeleteConfirmed_Unknown_StillRedirects()
        {
            using var ctx = TestDb.CreateInMemory();
            var result = await Create(ctx).DeleteConfirmed(999);
            Assert.IsType<RedirectToActionResult>(result);
        }
    }
}
