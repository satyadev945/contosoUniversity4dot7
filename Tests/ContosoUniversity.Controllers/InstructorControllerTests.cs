using ContosoUniversity.Controllers;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Tests;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class InstructorControllerTests
    {
        private static async Task<SchoolContext> SeededContext()
        {
            var ctx = TestHelpers.CreateContext();
            await DbInitializer.SeedAsync(ctx);
            ctx.ChangeTracker.Clear();
            return ctx;
        }

        private static InstructorController Create(SchoolContext ctx) =>
            new InstructorController(ctx, NullLogger<InstructorController>.Instance);

        [Fact]
        public async Task Index_NoParams_ReturnsAllInstructorsOrderedByLastName()
        {
            using var ctx = await SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Index(null, null));
            var model = Assert.IsType<InstructorIndexData>(view.Model);
            var names = model.Instructors.Select(i => i.LastName).ToList();
            Assert.Equal(5, names.Count);
            Assert.Equal(names.OrderBy(n => n).ToList(), names);
            Assert.Null(model.Courses);
            Assert.Null(model.Enrollments);
        }

        [Fact]
        public async Task Index_WithInstructorId_ReturnsCourses()
        {
            using var ctx = await SeededContext();
            var kapoor = await ctx.Instructors.SingleAsync(i => i.LastName == "Kapoor");
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Index(kapoor.ID, null));
            var model = Assert.IsType<InstructorIndexData>(view.Model);
            Assert.Equal(kapoor.ID, c.ViewBag.InstructorID);
            Assert.Single(model.Courses);
            Assert.Null(model.Enrollments);
        }

        [Fact]
        public async Task Index_WithUnknownInstructorId_ReturnsNotFound()
        {
            using var ctx = await SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Index(99999, null));
        }

        [Fact]
        public async Task Index_WithInstructorAndCourse_ReturnsEnrollments()
        {
            using var ctx = await SeededContext();
            var kapoor = await ctx.Instructors.SingleAsync(i => i.LastName == "Kapoor");
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Index(kapoor.ID, 1050));
            var model = Assert.IsType<InstructorIndexData>(view.Model);
            Assert.Equal(1050, c.ViewBag.CourseID);
            Assert.NotNull(model.Enrollments);
            Assert.Equal(3, model.Enrollments.Count());
            Assert.All(model.Enrollments, e => Assert.NotNull(e.Student));
        }

        [Fact]
        public async Task Index_WithUnknownCourse_ReturnsNotFound()
        {
            using var ctx = await SeededContext();
            var kapoor = await ctx.Instructors.SingleAsync(i => i.LastName == "Kapoor");
            Assert.IsType<NotFoundResult>(await Create(ctx).Index(kapoor.ID, 9999));
        }

        [Fact]
        public async Task Index_CourseWithoutInstructor_IgnoresCourse()
        {
            using var ctx = await SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Index(null, 1050));
            Assert.Null(Assert.IsType<InstructorIndexData>(view.Model).Enrollments);
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
            Assert.IsType<NotFoundResult>(await Create(ctx).Details(99999));
        }

        [Fact]
        public async Task Details_ExistingId_ReturnsInstructorWithOffice()
        {
            using var ctx = await SeededContext();
            var kapoor = await ctx.Instructors.SingleAsync(i => i.LastName == "Kapoor");
            var view = Assert.IsType<ViewResult>(await Create(ctx).Details(kapoor.ID));
            var model = Assert.IsType<Instructor>(view.Model);
            Assert.Equal("Thompson 304", model.OfficeAssignment.Location);
        }

        [Fact]
        public async Task CreateGet_PopulatesOfficeList()
        {
            using var ctx = await SeededContext();
            var c = Create(ctx);
            Assert.IsType<ViewResult>(await c.Create());
            Assert.IsType<SelectList>(c.ViewBag.ID);
        }

        [Fact]
        public async Task CreatePost_Valid_AddsAndRedirects()
        {
            using var ctx = await SeededContext();
            var result = await Create(ctx).Create(new Instructor { LastName = "New", FirstMidName = "Person", HireDate = DateTime.Today });
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Equal(6, await ctx.Instructors.CountAsync());
        }

        [Fact]
        public async Task CreatePost_InvalidModel_ReturnsView()
        {
            using var ctx = await SeededContext();
            var c = Create(ctx);
            c.ModelState.AddModelError("LastName", "Required");
            var model = new Instructor();
            var view = Assert.IsType<ViewResult>(await c.Create(model));
            Assert.Same(model, view.Model);
            Assert.Equal(5, await ctx.Instructors.CountAsync());
        }

        [Fact]
        public async Task CreatePost_SaveFails_AddsModelError()
        {
            using var ctx = TestHelpers.CreateThrowingContext();
            var c = new InstructorController(ctx, NullLogger<InstructorController>.Instance);
            var view = Assert.IsType<ViewResult>(await c.Create(new Instructor { LastName = "A", FirstMidName = "B" }));
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
            Assert.IsType<NotFoundResult>(await Create(ctx).Edit(99999));
        }

        [Fact]
        public async Task EditGet_ExistingId_ReturnsInstructor()
        {
            using var ctx = await SeededContext();
            var kapoor = await ctx.Instructors.SingleAsync(i => i.LastName == "Kapoor");
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Edit(kapoor.ID));
            Assert.Equal(kapoor.ID, Assert.IsType<Instructor>(view.Model).ID);
            Assert.NotNull(c.ViewBag.ID);
        }

        [Fact]
        public async Task EditPost_Valid_UpdatesAndRedirects()
        {
            using var ctx = await SeededContext();
            var kapoor = await ctx.Instructors.AsNoTracking().SingleAsync(i => i.LastName == "Kapoor");
            kapoor.FirstMidName = "Changed";
            var result = await Create(ctx).Edit(kapoor);
            Assert.IsType<RedirectToActionResult>(result);
            ctx.ChangeTracker.Clear();
            Assert.Equal("Changed", (await ctx.Instructors.FindAsync(kapoor.ID)).FirstMidName);
        }

        [Fact]
        public async Task EditPost_InvalidModel_ReturnsView()
        {
            using var ctx = await SeededContext();
            var c = Create(ctx);
            c.ModelState.AddModelError("LastName", "bad");
            var model = new Instructor { ID = 1 };
            var view = Assert.IsType<ViewResult>(await c.Edit(model));
            Assert.Same(model, view.Model);
        }

        [Fact]
        public async Task EditPost_SaveFails_AddsModelError()
        {
            using var ctx = await SeededContext();
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Edit(new Instructor { ID = 88888, LastName = "X", FirstMidName = "Y" }));
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
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete(99999));
        }

        [Fact]
        public async Task DeleteGet_ExistingId_ReturnsView()
        {
            using var ctx = await SeededContext();
            var kapoor = await ctx.Instructors.SingleAsync(i => i.LastName == "Kapoor");
            var view = Assert.IsType<ViewResult>(await Create(ctx).Delete(kapoor.ID));
            Assert.IsType<Instructor>(view.Model);
        }

        [Fact]
        public async Task DeleteConfirmed_UnknownId_ReturnsNotFound()
        {
            using var ctx = await SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).DeleteConfirmed(99999));
        }

        [Fact]
        public async Task DeleteConfirmed_ExistingId_RemovesAndRedirects()
        {
            using var ctx = await SeededContext();
            var zheng = await ctx.Instructors.SingleAsync(i => i.LastName == "Zheng");
            var result = await Create(ctx).DeleteConfirmed(zheng.ID);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Equal(4, await ctx.Instructors.CountAsync());
        }
    }
}
