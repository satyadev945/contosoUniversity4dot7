using System.Diagnostics;
using ContosoUniversity.Controllers;
using ContosoUniversity.Models;
using ContosoUniversity.Tests;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class HomeControllerTests
    {
        [Fact]
        public void Index_ReturnsView()
        {
            using var ctx = TestHelpers.CreateContext();
            var c = new HomeController(ctx);
            Assert.IsType<ViewResult>(c.Index());
        }

        [Fact]
        public void Contact_SetsMessageAndReturnsView()
        {
            using var ctx = TestHelpers.CreateContext();
            var c = new HomeController(ctx);
            var result = c.Contact();
            Assert.IsType<ViewResult>(result);
            Assert.Equal("Your contact page.", c.ViewBag.Message);
        }

        [Fact]
        public async Task About_GroupsStudentsByEnrollmentDate()
        {
            using var ctx = TestHelpers.CreateContext();
            var d1 = new DateTime(2012, 9, 1);
            var d2 = new DateTime(2013, 9, 1);
            ctx.Students.AddRange(
                new Student { LastName = "A", FirstMidName = "A", EnrollmentDate = d1 },
                new Student { LastName = "B", FirstMidName = "B", EnrollmentDate = d1 },
                new Student { LastName = "C", FirstMidName = "C", EnrollmentDate = d2 });
            await ctx.SaveChangesAsync();

            var c = new HomeController(ctx);
            var result = await c.About();

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<EnrollmentDataGroup>>(view.Model).ToList();
            Assert.Equal(2, model.Count);
            Assert.Equal(2, model.Single(g => g.EnrollmentDate == d1).StudentCount);
            Assert.Equal(1, model.Single(g => g.EnrollmentDate == d2).StudentCount);
        }

        [Fact]
        public async Task About_NoStudents_ReturnsEmptyModel()
        {
            using var ctx = TestHelpers.CreateContext();
            var c = new HomeController(ctx);
            var view = Assert.IsType<ViewResult>(await c.About());
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<EnrollmentDataGroup>>(view.Model));
        }

        [Fact]
        public void Error_UsesTraceIdentifierWhenNoActivity()
        {
            using var ctx = TestHelpers.CreateContext();
            var c = new HomeController(ctx).WithHttpContext();
            c.HttpContext.TraceIdentifier = "trace-123";
            Activity.Current = null;

            var view = Assert.IsType<ViewResult>(c.Error());
            var model = Assert.IsType<ErrorViewModel>(view.Model);
            Assert.Equal("trace-123", model.RequestId);
            Assert.True(model.ShowRequestId);
        }

        [Fact]
        public void Error_UsesActivityIdWhenPresent()
        {
            using var ctx = TestHelpers.CreateContext();
            var c = new HomeController(ctx).WithHttpContext();
            c.HttpContext.TraceIdentifier = "trace-123";
            using var activity = new Activity("test").Start();

            var view = Assert.IsType<ViewResult>(c.Error());
            var model = Assert.IsType<ErrorViewModel>(view.Model);
            Assert.Equal(activity.Id, model.RequestId);
        }
    }
}
