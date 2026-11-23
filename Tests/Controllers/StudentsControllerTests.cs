using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class StudentsControllerTests
    {
        private static StudentsController Create(SchoolContext ctx, Dictionary<string, string> form = null)
        {
            var c = new StudentsController(ctx, NullLogger<StudentsController>.Instance);
            ControllerTestHelper.Setup(c, form);
            return c;
        }

        private static List<Student> Students(IActionResult result)
        {
            return ControllerTestHelper.ModelOf<IEnumerable<Student>>(result).ToList();
        }

        [Fact]
        public async Task Index_Default_SortsByLastNameAndPagesBy3()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var controller = Create(ctx);
            var result = await controller.Index(null, null, null, null);
            var students = Students(result);
            Assert.Equal(new[] { "Alexander", "Alonso", "Anand" }, students.Select(s => s.LastName).ToArray());
            Assert.Equal("name_desc", controller.ViewBag.NameSortParm);
            Assert.Equal("Date", controller.ViewBag.DateSortParm);
        }

        [Fact]
        public async Task Index_SecondPage_ReturnsRemaining()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Index(null, null, null, 2);
            Assert.Equal(new[] { "Barzdukas", "Li" }, Students(result).Select(s => s.LastName).ToArray());
        }

        [Fact]
        public async Task Index_NameDesc_SortsDescending()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var controller = Create(ctx);
            var result = await controller.Index("name_desc", null, null, null);
            Assert.Equal("Li", Students(result).First().LastName);
            Assert.Equal("", controller.ViewBag.NameSortParm);
            Assert.Equal("name_desc", controller.ViewBag.CurrentSort);
        }

        [Fact]
        public async Task Index_Date_SortsByDateAscending()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var controller = Create(ctx);
            var result = await controller.Index("Date", null, null, null);
            Assert.Equal("Li", Students(result).First().LastName);
            Assert.Equal("date_desc", controller.ViewBag.DateSortParm);
        }

        [Fact]
        public async Task Index_DateDesc_SortsByDateDescending()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Index("date_desc", null, null, null);
            Assert.Equal("Barzdukas", Students(result).First().LastName);
        }

        [Fact]
        public async Task Index_SearchString_FiltersAndResetsPage()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var controller = Create(ctx);
            var result = await controller.Index(null, null, "Al", 2);
            var students = Students(result);
            Assert.Equal(2, students.Count);
            Assert.All(students, s => Assert.StartsWith("Al", s.LastName));
            Assert.Equal("Al", controller.ViewBag.CurrentFilter);
        }

        [Fact]
        public async Task Index_SearchByFirstName_Matches()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Index(null, null, "Yan", null);
            Assert.Equal("Li", Assert.Single(Students(result)).LastName);
        }

        [Fact]
        public async Task Index_NoSearchString_UsesCurrentFilter()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var controller = Create(ctx);
            var result = await controller.Index(null, "Li", null, null);
            Assert.Single(Students(result));
            Assert.Equal("Li", controller.ViewBag.CurrentFilter);
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
            Assert.IsType<NotFoundResult>(await Create(ctx).Details(100));
        }

        [Fact]
        public async Task Details_Existing_IncludesEnrollmentsAndCourse()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Details(1);
            var s = ControllerTestHelper.ModelOf<Student>(result);
            Assert.Single(s.Enrollments);
            Assert.Equal("Calculus", s.Enrollments.First().Course.Title);
        }

        [Fact]
        public void Create_Get_ReturnsView()
        {
            using var ctx = TestDb.CreateInMemory();
            Assert.IsType<ViewResult>(Create(ctx).Create());
        }

        [Fact]
        public async Task Create_Post_Valid_AddsAndRedirects()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Create(new Student { LastName = "Newbie", FirstMidName = "Nora", EnrollmentDate = DateTime.Today });
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            using var verify = TestDb.CreateInMemory(db);
            Assert.Equal(6, await verify.Students.CountAsync());
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsView()
        {
            using var ctx = TestDb.CreateInMemory();
            var controller = Create(ctx);
            controller.ModelState.AddModelError("LastName", "required");
            var student = new Student();
            Assert.Same(student, ControllerTestHelper.ModelOf<Student>(await controller.Create(student)));
            Assert.Equal(0, await ctx.Students.CountAsync());
        }

        [Fact]
        public async Task Create_Post_RetryLimitExceeded_AddsModelError()
        {
            using var ctx = new ThrowingSchoolContext(TestDb.InMemoryOptions(), new RetryLimitExceededException("retry", new Exception("inner")));
            var controller = Create(ctx);
            var result = await controller.Create(new Student { LastName = "A", FirstMidName = "B", EnrollmentDate = DateTime.Today });
            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains("Unable to save changes", controller.ModelState[""].Errors[0].ErrorMessage);
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
            Assert.IsType<NotFoundResult>(await Create(ctx).Edit((int?)55));
        }

        [Fact]
        public async Task Edit_Get_Existing_ReturnsStudent()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Edit((int?)5);
            Assert.Equal("Li", ControllerTestHelper.ModelOf<Student>(result).LastName);
        }

        [Fact]
        public async Task EditPost_NullId_ReturnsBadRequest()
        {
            using var ctx = TestDb.CreateInMemory();
            Assert.IsType<BadRequestResult>(await Create(ctx).EditPost(null));
        }

        [Fact]
        public async Task EditPost_Unknown_ReturnsNotFound()
        {
            using var ctx = TestDb.CreateInMemory();
            Assert.IsType<NotFoundResult>(await Create(ctx).EditPost(88));
        }

        [Fact]
        public async Task EditPost_ValidForm_UpdatesAndRedirects()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var form = new Dictionary<string, string>
            {
                ["LastName"] = "Updated",
                ["FirstMidName"] = "Name",
                ["EnrollmentDate"] = "2014-01-02"
            };
            var result = await Create(ctx, form).EditPost(5);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            using var verify = TestDb.CreateInMemory(db);
            var s = await verify.Students.FindAsync(5);
            Assert.Equal("Updated", s.LastName);
            Assert.Equal(new DateTime(2014, 1, 2), s.EnrollmentDate);
        }

        [Fact]
        public async Task EditPost_InvalidForm_ReturnsView()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var form = new Dictionary<string, string>
            {
                ["LastName"] = new string('x', 80),
                ["FirstMidName"] = "Name",
                ["EnrollmentDate"] = "2014-01-02"
            };
            var result = await Create(ctx, form).EditPost(5);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task EditPost_RetryLimitExceeded_AddsModelError()
        {
            var db = TestDb.SeedBasic();
            using var ctx = new ThrowingSchoolContext(TestDb.InMemoryOptions(db), new RetryLimitExceededException("retry", new Exception("inner")));
            var form = new Dictionary<string, string>
            {
                ["LastName"] = "Updated",
                ["FirstMidName"] = "Name",
                ["EnrollmentDate"] = "2014-01-02"
            };
            var controller = Create(ctx, form);
            var result = await controller.EditPost(5);
            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Delete_Get_NullId_ReturnsBadRequest()
        {
            using var ctx = TestDb.CreateInMemory();
            Assert.IsType<BadRequestResult>(await Create(ctx).Delete(null, false));
        }

        [Fact]
        public async Task Delete_Get_Unknown_ReturnsNotFound()
        {
            using var ctx = TestDb.CreateInMemory();
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete(12, false));
        }

        [Fact]
        public async Task Delete_Get_Existing_ReturnsStudentWithoutError()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var controller = Create(ctx);
            var result = await controller.Delete((int?)1, (bool?)false);
            Assert.Equal(1, ControllerTestHelper.ModelOf<Student>(result).ID);
            Assert.Null(controller.ViewBag.ErrorMessage);
        }

        [Fact]
        public async Task Delete_Get_SaveChangesError_SetsErrorMessage()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var controller = Create(ctx);
            await controller.Delete((int?)1, (bool?)true);
            Assert.Contains("Delete failed", (string)controller.ViewBag.ErrorMessage);
        }

        [Fact]
        public async Task Delete_Post_Existing_RemovesAndRedirects()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Delete(4);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            using var verify = TestDb.CreateInMemory(db);
            Assert.Null(await verify.Students.FindAsync(4));
        }

        [Fact]
        public async Task Delete_Post_Unknown_StillRedirectsToIndex()
        {
            using var ctx = TestDb.CreateInMemory();
            var result = await Create(ctx).Delete(404);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
        }

        [Fact]
        public async Task Delete_Post_DbUpdateException_RedirectsToDeleteWithError()
        {
            var db = TestDb.SeedBasic();
            using var ctx = new ThrowingSchoolContext(TestDb.InMemoryOptions(db), new DbUpdateException("fail", new Exception("inner")));
            var result = await Create(ctx).Delete(1);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Delete", redirect.ActionName);
            Assert.Equal(1, redirect.RouteValues["id"]);
            Assert.Equal(true, redirect.RouteValues["saveChangesError"]);
        }
    }
}
