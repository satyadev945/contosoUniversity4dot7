using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class StudentsControllerTests : IDisposable
    {
        private readonly TestDb _db = new TestDb().Seed();

        public void Dispose() => _db.Dispose();

        private StudentsController Create(SchoolContext ctx, ILogger<StudentsController> logger = null) =>
            new StudentsController(ctx, logger ?? NullLogger<StudentsController>.Instance);

        private int IdOf(string lastName)
        {
            using var ctx = _db.CreateContext();
            return ctx.Students.Single(s => s.LastName == lastName).ID;
        }

        private static PaginatedList<Student> Model(IActionResult r) =>
            Assert.IsType<PaginatedList<Student>>(Assert.IsType<ViewResult>(r).Model);

        // ---- Index ----

        [Fact]
        public async Task Index_Default_SortsByLastNameAndPagesBy3()
        {
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            var model = Model(await controller.Index(null, null, null, null));
            Assert.Equal(3, model.Count);
            Assert.Equal(new[] { "Alexander", "Alonso", "Anand" }, model.Select(s => s.LastName).ToArray());
            Assert.Equal(2, model.TotalPages);
            Assert.Equal("name_desc", controller.ViewData["NameSortParm"]);
            Assert.Equal("Date", controller.ViewData["DateSortParm"]);
        }

        [Fact]
        public async Task Index_NameDesc_SortsDescending()
        {
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            var model = Model(await controller.Index("name_desc", null, null, null));
            Assert.Equal("Norman", model[0].LastName);
            Assert.Equal("name_desc", controller.ViewData["CurrentSort"]);
            Assert.Equal("", controller.ViewData["NameSortParm"]);
        }

        [Fact]
        public async Task Index_DateAscending_SortsByEnrollmentDate()
        {
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            var model = Model(await controller.Index("Date", null, null, null));
            Assert.Equal("Alexander", model[0].LastName);
            Assert.Equal("date_desc", controller.ViewData["DateSortParm"]);
        }

        [Fact]
        public async Task Index_DateDesc_SortsByEnrollmentDateDescending()
        {
            using var ctx = _db.CreateContext();
            var model = Model(await Create(ctx).Index("date_desc", null, null, null));
            Assert.Equal(new DateTime(2013, 9, 1), model[0].EnrollmentDate);
        }

        [Fact]
        public async Task Index_SearchString_FiltersAndResetsPage()
        {
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            var model = Model(await controller.Index(null, null, "Al", 2));
            Assert.Equal(1, model.PageIndex);
            Assert.Equal(2, model.TotalCount);
            Assert.Equal("Al", controller.ViewData["CurrentFilter"]);
        }

        [Fact]
        public async Task Index_SearchMatchesFirstName()
        {
            using var ctx = _db.CreateContext();
            var model = Model(await Create(ctx).Index(null, null, "Laura", null));
            Assert.Single(model);
            Assert.Equal("Norman", model[0].LastName);
        }

        [Fact]
        public async Task Index_NoSearchString_UsesCurrentFilterAndPage()
        {
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            var model = Model(await controller.Index(null, "Al", null, 1));
            Assert.Equal(2, model.TotalCount);
            Assert.Equal("Al", controller.ViewData["CurrentFilter"]);
        }

        [Fact]
        public async Task Index_SecondPage_ReturnsRemainingStudents()
        {
            using var ctx = _db.CreateContext();
            var model = Model(await Create(ctx).Index(null, null, null, 2));
            Assert.Equal(2, model.Count);
            Assert.True(model.HasPreviousPage);
            Assert.False(model.HasNextPage);
        }

        // ---- Details ----

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
        public async Task Details_Existing_IncludesEnrollmentsAndCourses()
        {
            int id = IdOf("Alonso");
            using var ctx = _db.CreateContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Details(id));
            var student = Assert.IsType<Student>(view.Model);
            Assert.Equal(2, student.Enrollments.Count);
            Assert.All(student.Enrollments, e => Assert.NotNull(e.Course));
        }

        // ---- Create ----

        [Fact]
        public void CreateGet_ReturnsView()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<ViewResult>(Create(ctx).Create());
        }

        [Fact]
        public async Task CreatePost_Valid_PersistsAndRedirects()
        {
            using (var ctx = _db.CreateContext())
            {
                var result = await Create(ctx).Create(new Student { LastName = "New", FirstMidName = "Student", EnrollmentDate = new DateTime(2020, 1, 1) });
                Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            }
            using var verify = _db.CreateContext();
            Assert.Equal(6, await verify.Students.CountAsync());
        }

        [Fact]
        public async Task CreatePost_InvalidModelState_ReturnsViewWithoutSaving()
        {
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            controller.ModelState.AddModelError("LastName", "Required");
            var student = new Student();
            var view = Assert.IsType<ViewResult>(await controller.Create(student));
            Assert.Same(student, view.Model);
            Assert.Equal(5, await ctx.Students.CountAsync());
        }

        [Fact]
        public async Task CreatePost_DbUpdateException_AddsModelErrorAndLogs()
        {
            using var ctx = _db.CreateThrowingContext();
            var logger = new CapturingLogger<StudentsController>();
            var controller = Create(ctx, logger);
            var view = Assert.IsType<ViewResult>(await controller.Create(new Student { LastName = "A", FirstMidName = "B" }));
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
        }

        [Fact]
        public async Task CreatePost_RetryLimitExceeded_AddsModelError()
        {
            using var ctx = _db.CreateThrowingContext(new RetryLimitExceededException("retry", new Exception("inner")));
            var controller = Create(ctx);
            Assert.IsType<ViewResult>(await controller.Create(new Student { LastName = "A", FirstMidName = "B" }));
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task CreatePost_UnexpectedException_Propagates()
        {
            using var ctx = _db.CreateThrowingContext(new InvalidOperationException("unexpected"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => Create(ctx).Create(new Student { LastName = "A", FirstMidName = "B" }));
        }

        // ---- Edit GET ----

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
        public async Task EditGet_Existing_ReturnsStudent()
        {
            int id = IdOf("Li");
            using var ctx = _db.CreateContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Edit((int?)id));
            Assert.Equal("Li", Assert.IsType<Student>(view.Model).LastName);
        }

        // ---- Edit POST ----

        private static Dictionary<string, string> Form(string last, string first, string date) => new()
        {
            ["LastName"] = last,
            ["FirstMidName"] = first,
            ["EnrollmentDate"] = date
        };

        [Fact]
        public async Task EditPost_NullId_ReturnsBadRequest()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).EditPost(null));
        }

        [Fact]
        public async Task EditPost_Unknown_ReturnsNotFound()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).EditPost(99999));
        }

        [Fact]
        public async Task EditPost_ValidForm_UpdatesStudent()
        {
            int id = IdOf("Li");
            using (var ctx = _db.CreateContext())
            {
                var controller = Create(ctx);
                TestDb.AttachFormContext(controller, Form("Lee", "Yan", "2015-05-05"));
                var result = await controller.EditPost(id);
                Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            }
            using var verify = _db.CreateContext();
            var s = await verify.Students.FindAsync(id);
            Assert.Equal("Lee", s.LastName);
            Assert.Equal(new DateTime(2015, 5, 5), s.EnrollmentDate);
        }

        [Fact]
        public async Task EditPost_InvalidForm_ReturnsViewWithStudent()
        {
            int id = IdOf("Li");
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            TestDb.AttachFormContext(controller, Form("Lee", "Yan", "not-a-date"));
            var view = Assert.IsType<ViewResult>(await controller.EditPost(id));
            Assert.IsType<Student>(view.Model);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task EditPost_SaveFails_AddsModelError()
        {
            int id = IdOf("Li");
            using var ctx = _db.CreateThrowingContext();
            var controller = Create(ctx);
            TestDb.AttachFormContext(controller, Form("Lee", "Yan", "2015-05-05"));
            var view = Assert.IsType<ViewResult>(await controller.EditPost(id));
            Assert.Contains(controller.ModelState[""].Errors, e => e.ErrorMessage.StartsWith("Unable to save changes"));
        }

        // ---- Delete ----

        [Fact]
        public async Task DeleteGet_NullId_ReturnsBadRequest()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Delete((int?)null));
        }

        [Fact]
        public async Task DeleteGet_Unknown_ReturnsNotFound()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete((int?)99999));
        }

        [Fact]
        public async Task DeleteGet_Existing_ReturnsStudentWithoutError()
        {
            int id = IdOf("Norman");
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            var view = Assert.IsType<ViewResult>(await controller.Delete((int?)id));
            Assert.Equal("Norman", Assert.IsType<Student>(view.Model).LastName);
            Assert.Null(controller.ViewData["ErrorMessage"]);
        }

        [Fact]
        public async Task DeleteGet_WithSaveChangesError_SetsErrorMessage()
        {
            int id = IdOf("Norman");
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            Assert.IsType<ViewResult>(await controller.Delete((int?)id, true));
            Assert.StartsWith("Delete failed", (string)controller.ViewData["ErrorMessage"]);
        }

        [Fact]
        public async Task DeletePost_Existing_RemovesStudentAndEnrollments()
        {
            int id = IdOf("Alonso");
            using (var ctx = _db.CreateContext())
            {
                Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(await Create(ctx).Delete(id)).ActionName);
            }
            using var verify = _db.CreateContext();
            Assert.Null(await verify.Students.FindAsync(id));
            Assert.Empty(await verify.Enrollments.Where(e => e.StudentID == id).ToListAsync());
        }

        [Fact]
        public async Task DeletePost_Unknown_RedirectsToIndex()
        {
            using var ctx = _db.CreateContext();
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(await Create(ctx).Delete(99999)).ActionName);
            Assert.Equal(5, await ctx.Students.CountAsync());
        }

        [Fact]
        public async Task DeletePost_Failure_RedirectsToDeleteWithErrorFlag()
        {
            int id = IdOf("Alonso");
            using var ctx = _db.CreateThrowingContext(new DbUpdateException("fail"));
            var logger = new CapturingLogger<StudentsController>();
            var result = Assert.IsType<RedirectToActionResult>(await Create(ctx, logger).Delete(id));
            Assert.Equal("Delete", result.ActionName);
            Assert.Equal(true, result.RouteValues["saveChangesError"]);
            Assert.Equal(id, result.RouteValues["id"]);
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
        }
    }
}
