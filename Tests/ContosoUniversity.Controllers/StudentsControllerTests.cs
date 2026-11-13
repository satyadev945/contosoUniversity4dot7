using ContosoUniversity.Controllers;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Tests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using X.PagedList;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class StudentsControllerTests
    {
        private static async Task<SchoolContext> SeededContext()
        {
            var ctx = TestHelpers.CreateContext();
            await DbInitializer.SeedAsync(ctx);
            ctx.ChangeTracker.Clear();
            return ctx;
        }

        private static StudentsController Create(SchoolContext ctx) =>
            new StudentsController(ctx, NullLogger<StudentsController>.Instance);

        private static List<string> Names(IActionResult result)
        {
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Student>>(view.Model);
            return model.Select(s => s.LastName).ToList();
        }

        [Fact]
        public async Task Index_Default_SortsByLastNameAndPagesBy3()
        {
            using var ctx = await SeededContext();
            var c = Create(ctx);
            var result = await c.Index(null, null, null, null);
            var names = Names(result);
            Assert.Equal(new[] { "Alexander", "Alonso", "Anand" }, names);
            var paged = Assert.IsAssignableFrom<IPagedList<Student>>(((ViewResult)result).Model);
            Assert.Equal(8, paged.TotalItemCount);
            Assert.Equal(3, paged.PageCount);
            Assert.Equal("name_desc", c.ViewData["NameSortParm"]);
            Assert.Equal("Date", c.ViewData["DateSortParm"]);
        }

        [Fact]
        public async Task Index_NameDesc_SortsDescending()
        {
            using var ctx = await SeededContext();
            var c = Create(ctx);
            var names = Names(await c.Index("name_desc", null, null, null));
            Assert.Equal(new[] { "Olivetto", "Norman", "Li" }, names);
            Assert.Equal("", c.ViewData["NameSortParm"]);
            Assert.Equal("name_desc", c.ViewData["CurrentSort"]);
        }

        [Fact]
        public async Task Index_DateSort_SortsByEnrollmentDateAscending()
        {
            using var ctx = await SeededContext();
            var c = Create(ctx);
            var names = Names(await c.Index("Date", null, null, null));
            Assert.Equal("Olivetto", names[0]);
            Assert.Equal("date_desc", c.ViewData["DateSortParm"]);
        }

        [Fact]
        public async Task Index_DateDesc_SortsByEnrollmentDateDescending()
        {
            using var ctx = await SeededContext();
            var c = Create(ctx);
            var names = Names(await c.Index("date_desc", null, null, null));
            Assert.Contains(names[0], new[] { "Anand", "Norman" });
            Assert.Equal("Date", c.ViewData["DateSortParm"]);
        }

        [Fact]
        public async Task Index_SearchString_FiltersAndResetsPage()
        {
            using var ctx = await SeededContext();
            var c = Create(ctx);
            var result = await c.Index(null, null, "Al", 3);
            var names = Names(result);
            Assert.Equal(new[] { "Alexander", "Alonso" }, names);
            Assert.Equal("Al", c.ViewData["CurrentFilter"]);
            Assert.Equal(1, ((IPagedList<Student>)((ViewResult)result).Model).PageNumber);
        }

        [Fact]
        public async Task Index_SearchMatchesFirstName()
        {
            using var ctx = await SeededContext();
            var names = Names(await Create(ctx).Index(null, null, "Meredith", null));
            Assert.Equal(new[] { "Alonso" }, names);
        }

        [Fact]
        public async Task Index_NoSearchString_UsesCurrentFilter()
        {
            using var ctx = await SeededContext();
            var c = Create(ctx);
            var names = Names(await c.Index(null, "Li", null, null));
            Assert.Equal(new[] { "Li" }, names);
            Assert.Equal("Li", c.ViewData["CurrentFilter"]);
        }

        [Fact]
        public async Task Index_SecondPage_ReturnsNextThree()
        {
            using var ctx = await SeededContext();
            var names = Names(await Create(ctx).Index(null, null, null, 2));
            Assert.Equal(3, names.Count);
            Assert.Equal("Barzdukas", names[0]);
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
        public async Task Details_ExistingId_ReturnsStudentWithEnrollments()
        {
            using var ctx = await SeededContext();
            var alexander = await ctx.Students.SingleAsync(s => s.LastName == "Alexander");
            var view = Assert.IsType<ViewResult>(await Create(ctx).Details(alexander.ID));
            var model = Assert.IsType<Student>(view.Model);
            Assert.Equal(3, model.Enrollments.Count);
            Assert.All(model.Enrollments, e => Assert.NotNull(e.Course));
        }

        [Fact]
        public void CreateGet_ReturnsView()
        {
            using var ctx = TestHelpers.CreateContext();
            Assert.IsType<ViewResult>(Create(ctx).Create());
        }

        [Fact]
        public async Task CreatePost_Valid_AddsAndRedirects()
        {
            using var ctx = TestHelpers.CreateContext();
            var result = await Create(ctx).Create(new Student { LastName = "New", FirstMidName = "Student", EnrollmentDate = DateTime.Today });
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Equal(1, await ctx.Students.CountAsync());
        }

        [Fact]
        public async Task CreatePost_InvalidModel_ReturnsView()
        {
            using var ctx = TestHelpers.CreateContext();
            var c = Create(ctx);
            c.ModelState.AddModelError("LastName", "Required");
            var student = new Student();
            var view = Assert.IsType<ViewResult>(await c.Create(student));
            Assert.Same(student, view.Model);
            Assert.Equal(0, await ctx.Students.CountAsync());
        }

        [Fact]
        public async Task CreatePost_SaveFails_AddsModelError()
        {
            using var ctx = TestHelpers.CreateThrowingContext();
            var c = new StudentsController(ctx, NullLogger<StudentsController>.Instance);
            var view = Assert.IsType<ViewResult>(await c.Create(new Student { LastName = "A", FirstMidName = "B" }));
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
        public async Task EditGet_ExistingId_ReturnsStudent()
        {
            using var ctx = await SeededContext();
            var li = await ctx.Students.SingleAsync(s => s.LastName == "Li");
            var view = Assert.IsType<ViewResult>(await Create(ctx).Edit(li.ID));
            Assert.Equal("Li", Assert.IsType<Student>(view.Model).LastName);
        }

        [Fact]
        public async Task EditPost_NullId_ReturnsBadRequest()
        {
            using var ctx = await SeededContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).EditPost(null));
        }

        [Fact]
        public async Task EditPost_UnknownId_ReturnsNotFound()
        {
            using var ctx = await SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).EditPost(99999));
        }

        [Fact]
        public async Task EditPost_ValidForm_UpdatesStudentAndRedirects()
        {
            using var ctx = await SeededContext();
            var li = await ctx.Students.AsNoTracking().SingleAsync(s => s.LastName == "Li");
            var c = Create(ctx).WithHttpContext();
            c.SetForm(new Dictionary<string, string>
            {
                ["LastName"] = "Updated",
                ["FirstMidName"] = "Name",
                ["EnrollmentDate"] = "2014-01-01"
            });

            var result = await c.EditPost(li.ID);

            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            ctx.ChangeTracker.Clear();
            Assert.Equal("Updated", (await ctx.Students.FindAsync(li.ID)).LastName);
        }

        [Fact]
        public async Task EditPost_InvalidForm_ReturnsView()
        {
            using var ctx = await SeededContext();
            var li = await ctx.Students.AsNoTracking().SingleAsync(s => s.LastName == "Li");
            var c = Create(ctx).WithHttpContext();
            c.SetForm(new Dictionary<string, string>
            {
                ["LastName"] = "",
                ["FirstMidName"] = "Name",
                ["EnrollmentDate"] = "not a date"
            });

            var result = await c.EditPost(li.ID);

            var view = Assert.IsType<ViewResult>(result);
            Assert.IsType<Student>(view.Model);
            Assert.False(c.ModelState.IsValid);
        }

        [Fact]
        public async Task DeleteGet_NullId_ReturnsBadRequest()
        {
            using var ctx = await SeededContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Delete(null, false));
        }

        [Fact]
        public async Task DeleteGet_UnknownId_ReturnsNotFound()
        {
            using var ctx = await SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete(99999, false));
        }

        [Fact]
        public async Task DeleteGet_ExistingId_ReturnsStudentWithoutError()
        {
            using var ctx = await SeededContext();
            var li = await ctx.Students.SingleAsync(s => s.LastName == "Li");
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Delete(li.ID, false));
            Assert.Equal("Li", Assert.IsType<Student>(view.Model).LastName);
            Assert.Null(c.ViewData["ErrorMessage"]);
        }

        [Fact]
        public async Task DeleteGet_WithSaveChangesError_SetsErrorMessage()
        {
            using var ctx = await SeededContext();
            var li = await ctx.Students.SingleAsync(s => s.LastName == "Li");
            var c = Create(ctx);
            Assert.IsType<ViewResult>(await c.Delete(li.ID, true));
            Assert.Contains("Delete failed", (string)c.ViewData["ErrorMessage"]);
        }

        [Fact]
        public async Task DeletePost_UnknownId_ReturnsNotFound()
        {
            using var ctx = await SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete(99999));
        }

        [Fact]
        public async Task DeletePost_ExistingId_RemovesAndRedirectsToIndex()
        {
            using var ctx = await SeededContext();
            var li = await ctx.Students.SingleAsync(s => s.LastName == "Li");
            var result = await Create(ctx).Delete(li.ID);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Equal(7, await ctx.Students.CountAsync());
        }

        [Fact]
        public async Task DeletePost_SaveFails_RedirectsToDeleteWithError()
        {
            using var seed = TestHelpers.CreateContext("shared-delete");
            seed.Students.Add(new Student { LastName = "A", FirstMidName = "B", EnrollmentDate = DateTime.Today });
            await seed.SaveChangesAsync();
            var id = seed.Students.Single().ID;

            using var ctx = new ThrowingSchoolContext(TestHelpers.CreateOptions("shared-delete"));
            var c = new StudentsController(ctx, NullLogger<StudentsController>.Instance);
            var result = await c.Delete(id);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Delete", redirect.ActionName);
            Assert.Equal(true, redirect.RouteValues["saveChangesError"]);
        }
    }
}
