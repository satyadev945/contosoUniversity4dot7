using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class StudentsControllerTests
    {
        private static StudentsController Create(TestDb db, Dictionary<string, string>? form = null) =>
            new StudentsController(db.CreateContext(), TestDb.NullLog<StudentsController>()).WithContext(form);

        [Fact]
        public async Task Index_Default_SortsByLastNameAndPages()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            var result = Assert.IsType<ViewResult>(await c.Index(null, null, null, null));
            var model = Assert.IsAssignableFrom<IPagedList<Student>>(result.Model);
            Assert.Equal(3, model.Count);
            Assert.Equal(2, model.PageCount);
            Assert.Equal("Alexander", model.First().LastName);
            Assert.Equal("name_desc", c.ViewData["NameSortParm"]);
            Assert.Equal("Date", c.ViewData["DateSortParm"]);
        }

        [Fact]
        public async Task Index_NameDesc_SortsDescending()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            var result = Assert.IsType<ViewResult>(await c.Index("name_desc", null, null, null));
            var model = Assert.IsAssignableFrom<IPagedList<Student>>(result.Model);
            Assert.Equal("Li", model.First().LastName);
            Assert.Equal("", c.ViewData["NameSortParm"]);
            Assert.Equal("name_desc", c.ViewData["CurrentSort"]);
        }

        [Fact]
        public async Task Index_DateSort_Ascending()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            var result = Assert.IsType<ViewResult>(await c.Index("Date", null, null, null));
            var model = Assert.IsAssignableFrom<IPagedList<Student>>(result.Model);
            Assert.Equal("Alexander", model.First().LastName);
            Assert.Equal("date_desc", c.ViewData["DateSortParm"]);
        }

        [Fact]
        public async Task Index_DateDesc_Descending()
        {
            using var db = new TestDb();
            db.Seed();
            var result = Assert.IsType<ViewResult>(await Create(db).Index("date_desc", null, null, null));
            var model = Assert.IsAssignableFrom<IPagedList<Student>>(result.Model);
            Assert.Equal("Anand", model.First().LastName);
        }

        [Fact]
        public async Task Index_SearchString_FiltersAndResetsPage()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            var result = Assert.IsType<ViewResult>(await c.Index(null, null, "Al", 2));
            var model = Assert.IsAssignableFrom<IPagedList<Student>>(result.Model);
            Assert.Equal(1, model.PageNumber);
            Assert.Equal(2, model.Count);
            Assert.Equal("Al", c.ViewData["CurrentFilter"]);
        }

        [Fact]
        public async Task Index_UsesCurrentFilterWhenNoSearchString()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            var result = Assert.IsType<ViewResult>(await c.Index(null, "Li", null, 1));
            var model = Assert.IsAssignableFrom<IPagedList<Student>>(result.Model);
            Assert.Single(model);
            Assert.Equal("Li", c.ViewData["CurrentFilter"]);
        }

        [Fact]
        public async Task Index_SecondPage()
        {
            using var db = new TestDb();
            db.Seed();
            var result = Assert.IsType<ViewResult>(await Create(db).Index(null, null, null, 2));
            var model = Assert.IsAssignableFrom<IPagedList<Student>>(result.Model);
            Assert.Equal(2, model.Count);
            Assert.Equal(2, model.PageNumber);
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
            Assert.IsType<NotFoundResult>(await Create(db).Details(1));
        }

        [Fact]
        public async Task Details_Existing_IncludesEnrollmentsAndCourses()
        {
            using var db = new TestDb();
            db.Seed();
            var result = Assert.IsType<ViewResult>(await Create(db).Details(1));
            var s = Assert.IsType<Student>(result.Model);
            Assert.Equal(2, s.Enrollments.Count);
            Assert.All(s.Enrollments, e => Assert.NotNull(e.Course));
        }

        [Fact]
        public void CreateGet_ReturnsView()
        {
            using var db = new TestDb();
            Assert.IsType<ViewResult>(Create(db).Create());
        }

        [Fact]
        public async Task CreatePost_Valid_Persists()
        {
            using var db = new TestDb();
            var s = new Student { LastName = "New", FirstMidName = "Student", EnrollmentDate = new DateTime(2020, 1, 1) };
            var result = await Create(db).Create(s);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            using var ctx = db.CreateContext();
            Assert.Equal(1, await ctx.Students.CountAsync());
        }

        [Fact]
        public async Task CreatePost_InvalidModel_ReturnsView()
        {
            using var db = new TestDb();
            var c = Create(db);
            c.ModelState.AddModelError("LastName", "required");
            var result = await c.Create(new Student());
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task CreatePost_DbUpdateException_AddsModelError()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            var result = await c.Create(new Student { ID = 1, LastName = "Dup", FirstMidName = "X" });
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
            Assert.IsType<NotFoundResult>(await Create(db).Edit(1));
        }

        [Fact]
        public async Task EditGet_Existing_ReturnsView()
        {
            using var db = new TestDb();
            db.Seed();
            var result = Assert.IsType<ViewResult>(await Create(db).Edit(3));
            Assert.Equal("Anand", Assert.IsType<Student>(result.Model).LastName);
        }

        [Fact]
        public async Task EditPost_NullId_BadRequest()
        {
            using var db = new TestDb();
            Assert.IsType<BadRequestResult>(await Create(db).EditPost(null));
        }

        [Fact]
        public async Task EditPost_Missing_NotFound()
        {
            using var db = new TestDb();
            Assert.IsType<NotFoundResult>(await Create(db).EditPost(1));
        }

        [Fact]
        public async Task EditPost_ValidForm_UpdatesStudent()
        {
            using var db = new TestDb();
            db.Seed();
            var form = new Dictionary<string, string>
            {
                ["LastName"] = "Updated",
                ["FirstMidName"] = "Name",
                ["EnrollmentDate"] = "2015-05-05"
            };
            var result = await Create(db, form).EditPost(1);
            Assert.IsType<RedirectToActionResult>(result);
            using var ctx = db.CreateContext();
            var s = await ctx.Students.FindAsync(1);
            Assert.Equal("Updated", s!.LastName);
            Assert.Equal(new DateTime(2015, 5, 5), s.EnrollmentDate);
        }

        [Fact]
        public async Task EditPost_InvalidForm_ReturnsView()
        {
            using var db = new TestDb();
            db.Seed();
            var form = new Dictionary<string, string>
            {
                ["LastName"] = "",
                ["FirstMidName"] = "Name",
                ["EnrollmentDate"] = "not-a-date"
            };
            var result = await Create(db, form).EditPost(1);
            Assert.IsType<ViewResult>(result);
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
            Assert.IsType<NotFoundResult>(await Create(db).Delete(1, false));
        }

        [Fact]
        public async Task DeleteGet_Existing_ReturnsView()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            Assert.IsType<ViewResult>(await c.Delete(1, false));
            Assert.Null(c.ViewData["ErrorMessage"]);
        }

        [Fact]
        public async Task DeleteGet_SaveChangesError_SetsMessage()
        {
            using var db = new TestDb();
            db.Seed();
            var c = Create(db);
            Assert.IsType<ViewResult>(await c.Delete(1, true));
            Assert.NotNull(c.ViewData["ErrorMessage"]);
        }

        [Fact]
        public async Task DeletePost_Missing_NotFound()
        {
            using var db = new TestDb();
            Assert.IsType<NotFoundResult>(await Create(db).Delete(1));
        }

        [Fact]
        public async Task DeletePost_Existing_RemovesStudent()
        {
            using var db = new TestDb();
            db.Seed();
            var result = await Create(db).Delete(5);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            using var ctx = db.CreateContext();
            Assert.Null(await ctx.Students.FindAsync(5));
        }
    }
}
