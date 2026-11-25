using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using X.PagedList;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class StudentsControllerTests
    {
        private static StudentsController Create(SchoolContext ctx, Dictionary<string, string> form = null) =>
            new StudentsController(ctx, NullLogger<StudentsController>.Instance)
            {
                ControllerContext = TestDb.CreateControllerContext(form)
            };

        private static IPagedList<Student> Model(IActionResult result) =>
            Assert.IsAssignableFrom<IPagedList<Student>>(Assert.IsType<ViewResult>(result).Model);

        [Fact]
        public async Task Index_Default_SortsByLastNameAndPagesBy3()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);

            var model = Model(await controller.Index(null, null, null, null));

            Assert.Equal(3, model.Count);
            Assert.Equal(5, model.TotalItemCount);
            Assert.Equal("Alexander", model[0].LastName);
            Assert.Equal("name_desc", (string)controller.ViewBag.NameSortParm);
            Assert.Equal("Date", (string)controller.ViewBag.DateSortParm);
        }

        [Fact]
        public async Task Index_SecondPage_ReturnsRemainingStudents()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var model = Model(await Create(ctx).Index(null, null, null, 2));
            Assert.Equal(2, model.Count);
            Assert.Equal(2, model.PageNumber);
        }

        [Fact]
        public async Task Index_NameDescSort_ReturnsLastNameDescending()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);

            var model = Model(await controller.Index("name_desc", null, null, null));

            Assert.Equal("Li", model[0].LastName);
            Assert.Equal("name_desc", (string)controller.ViewBag.CurrentSort);
            Assert.Equal("", (string)controller.ViewBag.NameSortParm);
        }

        [Fact]
        public async Task Index_DateSort_ReturnsOldestFirst()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);

            var model = Model(await controller.Index("Date", null, null, null));

            Assert.Equal(2010, model[0].EnrollmentDate.Year);
            Assert.Equal("date_desc", (string)controller.ViewBag.DateSortParm);
        }

        [Fact]
        public async Task Index_DateDescSort_ReturnsNewestFirst()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var model = Model(await Create(ctx).Index("date_desc", null, null, null));
            Assert.Equal(2013, model[0].EnrollmentDate.Year);
        }

        [Fact]
        public async Task Index_SearchString_FiltersAndResetsPage()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);

            var model = Model(await controller.Index(null, null, "Li", 5));

            Assert.Equal("Li", Assert.Single(model).LastName);
            Assert.Equal(1, model.PageNumber);
            Assert.Equal("Li", (string)controller.ViewBag.CurrentFilter);
        }

        [Fact]
        public async Task Index_SearchMatchesFirstName()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var model = Model(await Create(ctx).Index(null, null, "Carson", null));
            Assert.Equal("Alexander", Assert.Single(model).LastName);
        }

        [Fact]
        public async Task Index_NullSearchString_UsesCurrentFilter()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);

            var model = Model(await controller.Index(null, "Anand", null, null));

            Assert.Equal("Anand", Assert.Single(model).LastName);
            Assert.Equal("Anand", (string)controller.ViewBag.CurrentFilter);
        }

        [Fact]
        public async Task Details_NullId_ReturnsBadRequest()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            Assert.IsType<BadRequestResult>(await Create(ctx).Details(null));
        }

        [Fact]
        public async Task Details_Unknown_ReturnsNotFound()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            Assert.IsType<NotFoundResult>(await Create(ctx).Details(999));
        }

        [Fact]
        public async Task Details_Known_ReturnsStudentWithEnrollmentsAndCourses()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var result = await Create(ctx).Details(1);
            var student = Assert.IsType<Student>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(2, student.Enrollments.Count);
            Assert.All(student.Enrollments, e => Assert.NotNull(e.Course));
        }

        [Fact]
        public void CreateGet_ReturnsView()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            Assert.IsType<ViewResult>(Create(ctx).Create());
        }

        [Fact]
        public async Task CreatePost_Valid_PersistsAndRedirects()
        {
            var name = TestDb.CreateSeeded();
            using (var ctx = TestDb.Create(name))
            {
                var result = await Create(ctx).Create(new Student { LastName = "New", FirstMidName = "Student", EnrollmentDate = new DateTime(2020, 1, 1) });
                Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            }
            using var verify = TestDb.Create(name);
            Assert.Equal(6, verify.Students.Count());
        }

        [Fact]
        public async Task CreatePost_InvalidModelState_ReturnsView()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);
            controller.ModelState.AddModelError("LastName", "Required");
            var student = new Student();

            var result = await controller.Create(student);

            Assert.Same(student, Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(5, ctx.Students.Count());
        }

        [Fact]
        public async Task CreatePost_DbUpdateException_AddsModelError()
        {
            using var ctx = TestDb.CreateThrowing(TestDb.CreateSeeded(), new DbUpdateException("boom"));
            var controller = Create(ctx);

            var result = await controller.Create(new Student { LastName = "A", FirstMidName = "B" });

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState[""].Errors, e => e.ErrorMessage.Contains("Unable to save changes"));
        }

        [Fact]
        public async Task CreatePost_RetryLimitExceeded_AddsModelError()
        {
            using var ctx = TestDb.CreateThrowing(TestDb.CreateSeeded(), new RetryLimitExceededException("retry", new Exception()));
            var controller = Create(ctx);

            var result = await controller.Create(new Student { LastName = "A", FirstMidName = "B" });

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task EditGet_NullId_ReturnsBadRequest()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            Assert.IsType<BadRequestResult>(await Create(ctx).Edit((int?)null));
        }

        [Fact]
        public async Task EditGet_Unknown_ReturnsNotFound()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            Assert.IsType<NotFoundResult>(await Create(ctx).Edit(999));
        }

        [Fact]
        public async Task EditGet_Known_ReturnsStudent()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var result = await Create(ctx).Edit(3);
            Assert.Equal("Anand", Assert.IsType<Student>(Assert.IsType<ViewResult>(result).Model).LastName);
        }

        [Fact]
        public async Task EditPost_NullId_ReturnsBadRequest()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            Assert.IsType<BadRequestResult>(await Create(ctx).EditPost(null));
        }

        [Fact]
        public async Task EditPost_Unknown_ReturnsNotFound()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            Assert.IsType<NotFoundResult>(await Create(ctx).EditPost(999));
        }

        [Fact]
        public async Task EditPost_ValidForm_UpdatesAndRedirects()
        {
            var name = TestDb.CreateSeeded();
            var form = new Dictionary<string, string>
            {
                ["LastName"] = "Updated", ["FirstMidName"] = "Name", ["EnrollmentDate"] = "2015-05-05"
            };
            using (var ctx = TestDb.Create(name))
            {
                var result = await Create(ctx, form).EditPost(2);
                Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            }
            using var verify = TestDb.Create(name);
            var s = verify.Students.Single(x => x.ID == 2);
            Assert.Equal("Updated", s.LastName);
            Assert.Equal("Name", s.FirstMidName);
            Assert.Equal(new DateTime(2015, 5, 5), s.EnrollmentDate);
        }

        [Fact]
        public async Task EditPost_InvalidForm_ReturnsView()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var form = new Dictionary<string, string>
            {
                ["LastName"] = "", ["FirstMidName"] = "Name", ["EnrollmentDate"] = "2015-05-05"
            };

            var result = await Create(ctx, form).EditPost(2);

            Assert.IsType<Student>(Assert.IsType<ViewResult>(result).Model);
        }

        [Fact]
        public async Task EditPost_DbUpdateException_AddsModelError()
        {
            using var ctx = TestDb.CreateThrowing(TestDb.CreateSeeded(), new DbUpdateException("boom"));
            var form = new Dictionary<string, string>
            {
                ["LastName"] = "Updated", ["FirstMidName"] = "Name", ["EnrollmentDate"] = "2015-05-05"
            };
            var controller = Create(ctx, form);

            var result = await controller.EditPost(2);

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState[""].Errors, e => e.ErrorMessage.Contains("Unable to save changes"));
        }

        [Fact]
        public async Task DeleteGet_NullId_ReturnsBadRequest()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            Assert.IsType<BadRequestResult>(await Create(ctx).Delete(null));
        }

        [Fact]
        public async Task DeleteGet_Unknown_ReturnsNotFound()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete((int?)999, false));
        }

        [Fact]
        public async Task DeleteGet_Known_ReturnsStudentWithoutErrorMessage()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);

            var result = await controller.Delete((int?)1, false);

            Assert.IsType<Student>(Assert.IsType<ViewResult>(result).Model);
            Assert.Null(controller.ViewBag.ErrorMessage);
        }

        [Fact]
        public async Task DeleteGet_WithSaveChangesError_SetsErrorMessage()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);

            await controller.Delete(1, true);

            Assert.Contains("Delete failed", (string)controller.ViewBag.ErrorMessage);
        }

        [Fact]
        public async Task DeletePost_Existing_RemovesAndRedirectsToIndex()
        {
            var name = TestDb.CreateSeeded();
            using (var ctx = TestDb.Create(name))
            {
                var result = await Create(ctx).Delete(4);
                Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            }
            using var verify = TestDb.Create(name);
            Assert.Null(verify.Students.Find(4));
        }

        [Fact]
        public async Task DeletePost_Missing_RedirectsToIndex()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var result = await Create(ctx).Delete(999);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Equal(5, ctx.Students.Count());
        }

        [Fact]
        public async Task DeletePost_DbUpdateException_RedirectsToDeleteWithError()
        {
            using var ctx = TestDb.CreateThrowing(TestDb.CreateSeeded(), new DbUpdateException("boom"));

            var result = await Create(ctx).Delete(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Delete", redirect.ActionName);
            Assert.Equal(true, redirect.RouteValues["saveChangesError"]);
            Assert.Equal(1, redirect.RouteValues["id"]);
        }

        [Fact]
        public async Task DeletePost_RetryLimitExceeded_RedirectsToDeleteWithError()
        {
            using var ctx = TestDb.CreateThrowing(TestDb.CreateSeeded(), new RetryLimitExceededException("retry", new Exception()));
            var redirect = Assert.IsType<RedirectToActionResult>(await Create(ctx).Delete(1));
            Assert.Equal("Delete", redirect.ActionName);
        }
    }
}
