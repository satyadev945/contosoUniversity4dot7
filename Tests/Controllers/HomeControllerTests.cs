using ContosoUniversity.Tests.Helpers;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class HomeControllerTests
    {
        [Fact]
        public void Index_ReturnsView()
        {
            using var db = new TestDb();
            using var ctx = db.CreateContext();
            Assert.IsType<ViewResult>(new HomeController(ctx, NullLogger<HomeController>.Instance).Index());
        }

        [Fact]
        public void Contact_SetsMessage()
        {
            using var db = new TestDb();
            using var ctx = db.CreateContext();
            var controller = new HomeController(ctx, NullLogger<HomeController>.Instance);
            Assert.IsType<ViewResult>(controller.Contact());
            Assert.Equal("Your contact page.", controller.ViewData["Message"]);
        }

        [Fact]
        public async Task About_GroupsStudentsByEnrollmentDate()
        {
            using var db = new TestDb().Seed();
            using var ctx = db.CreateContext();
            var view = Assert.IsType<ViewResult>(await new HomeController(ctx, NullLogger<HomeController>.Instance).About());
            var data = Assert.IsAssignableFrom<IEnumerable<EnrollmentDataGroup>>(view.Model).ToList();
            Assert.Equal(3, data.Count);
            Assert.Equal(5, data.Sum(d => d.StudentCount));
            Assert.Equal(2, data.Single(d => d.EnrollmentDate == new DateTime(2012, 9, 1)).StudentCount);
        }

        [Fact]
        public async Task About_EmptyDatabase_ReturnsEmptyModel()
        {
            using var db = new TestDb();
            using var ctx = db.CreateContext();
            var view = Assert.IsType<ViewResult>(await new HomeController(ctx, NullLogger<HomeController>.Instance).About());
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<EnrollmentDataGroup>>(view.Model));
        }

        [Fact]
        public void Error_UsesTraceIdentifierAndLogs()
        {
            using var db = new TestDb();
            using var ctx = db.CreateContext();
            var logger = new CapturingLogger<HomeController>();
            var controller = new HomeController(ctx, logger)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { TraceIdentifier = "trace-123" }
                }
            };
            var view = Assert.IsType<ViewResult>(controller.Error());
            var model = Assert.IsType<ErrorViewModel>(view.Model);
            Assert.Equal("trace-123", model.RequestId);
            Assert.True(model.ShowRequestId);
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("trace-123"));
        }

        [Fact]
        public void Error_WithActivity_UsesActivityId()
        {
            using var db = new TestDb();
            using var ctx = db.CreateContext();
            using var activity = new System.Diagnostics.Activity("test").Start();
            var controller = new HomeController(ctx, NullLogger<HomeController>.Instance)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { TraceIdentifier = "other" } }
            };
            var view = Assert.IsType<ViewResult>(controller.Error());
            Assert.Equal(activity.Id, Assert.IsType<ErrorViewModel>(view.Model).RequestId);
        }
    }
}
