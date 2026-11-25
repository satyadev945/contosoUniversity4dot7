using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
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
            using var ctx = TestDb.Create(TestDb.NewName());
            var controller = new HomeController(ctx);
            Assert.IsType<ViewResult>(controller.Index());
        }

        [Fact]
        public void Contact_SetsMessageAndReturnsView()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            var controller = new HomeController(ctx);

            var result = controller.Contact();

            Assert.IsType<ViewResult>(result);
            Assert.Equal("Your contact page.", (string)controller.ViewBag.Message);
        }

        [Fact]
        public async Task About_GroupsStudentsByEnrollmentDate()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = new HomeController(ctx);

            var result = await controller.About();

            var view = Assert.IsType<ViewResult>(result);
            var groups = Assert.IsAssignableFrom<List<EnrollmentDataGroup>>(view.Model);
            Assert.Equal(3, groups.Count);
            Assert.Equal(5, groups.Sum(g => g.StudentCount));
            Assert.Equal(2, groups.Single(g => g.EnrollmentDate == new DateTime(2012, 9, 1)).StudentCount);
            Assert.Equal(1, groups.Single(g => g.EnrollmentDate == new DateTime(2010, 9, 1)).StudentCount);
        }

        [Fact]
        public async Task About_EmptyDatabase_ReturnsEmptyList()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            var controller = new HomeController(ctx);

            var view = Assert.IsType<ViewResult>(await controller.About());

            Assert.Empty(Assert.IsAssignableFrom<List<EnrollmentDataGroup>>(view.Model));
        }

        [Fact]
        public void Error_UsesTraceIdentifierWhenNoActivity()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            var controller = new HomeController(ctx) { ControllerContext = TestDb.CreateControllerContext() };
            var previous = System.Diagnostics.Activity.Current;
            System.Diagnostics.Activity.Current = null;
            try
            {
                var result = controller.Error();

                var model = Assert.IsType<ErrorViewModel>(Assert.IsType<ViewResult>(result).Model);
                Assert.Equal("trace-1", model.RequestId);
                Assert.True(model.ShowRequestId);
            }
            finally
            {
                System.Diagnostics.Activity.Current = previous;
            }
        }

        [Fact]
        public void Error_UsesActivityIdWhenPresent()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            var controller = new HomeController(ctx) { ControllerContext = TestDb.CreateControllerContext() };
            using var activity = new System.Diagnostics.Activity("test");
            activity.Start();
            try
            {
                var result = controller.Error();

                var model = Assert.IsType<ErrorViewModel>(Assert.IsType<ViewResult>(result).Model);
                Assert.Equal(activity.Id, model.RequestId);
            }
            finally
            {
                activity.Stop();
            }
        }
    }
}
