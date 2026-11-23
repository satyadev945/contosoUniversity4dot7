using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class HomeControllerTests
    {
        private static HomeController Create(TestDb db) =>
            new HomeController(db.CreateContext(), TestDb.NullLog<HomeController>()).WithContext();

        [Fact]
        public void Index_ReturnsView()
        {
            using var db = new TestDb();
            Assert.IsType<ViewResult>(Create(db).Index());
        }

        [Fact]
        public async Task About_GroupsStudentsByEnrollmentDate()
        {
            using var db = new TestDb();
            db.Seed();
            var result = Assert.IsType<ViewResult>(await Create(db).About());
            var model = Assert.IsAssignableFrom<IEnumerable<EnrollmentDataGroup>>(result.Model).ToList();
            Assert.Equal(3, model.Count);
            Assert.Equal(3, model.Single(g => g.EnrollmentDate == new DateTime(2012, 9, 1)).StudentCount);
            Assert.Equal(5, model.Sum(g => g.StudentCount));
        }

        [Fact]
        public async Task About_EmptyDatabase_ReturnsEmptyModel()
        {
            using var db = new TestDb();
            var result = Assert.IsType<ViewResult>(await Create(db).About());
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<EnrollmentDataGroup>>(result.Model));
        }

        [Fact]
        public void Contact_SetsMessage()
        {
            using var db = new TestDb();
            var c = Create(db);
            var result = Assert.IsType<ViewResult>(c.Contact());
            Assert.Equal("Your contact page.", result.ViewData["Message"]);
        }

        [Fact]
        public void Error_ReturnsViewWithRequestId()
        {
            using var db = new TestDb();
            var c = Create(db);
            c.HttpContext.TraceIdentifier = "trace-123";
            var result = Assert.IsType<ViewResult>(c.Error());
            var model = Assert.IsType<ErrorViewModel>(result.Model);
            Assert.False(string.IsNullOrEmpty(model.RequestId));
            Assert.True(model.ShowRequestId);
        }
    }
}
