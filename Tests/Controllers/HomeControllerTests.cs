using System.Diagnostics;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class HomeControllerTests
    {
        private static HomeController Create(SchoolContext ctx)
        {
            var c = new HomeController(ctx);
            ControllerTestHelper.Setup(c);
            return c;
        }

        [Fact]
        public void Index_ReturnsView()
        {
            using var ctx = TestDb.CreateInMemory();
            var result = Create(ctx).Index();
            var view = Assert.IsType<ViewResult>(result);
            Assert.Null(view.Model);
        }

        [Fact]
        public async Task About_GroupsStudentsByEnrollmentDate()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).About();
            var data = ControllerTestHelper.ModelOf<IEnumerable<EnrollmentDataGroup>>(result).ToList();
            Assert.Equal(4, data.Count);
            Assert.Equal(5, data.Sum(d => d.StudentCount));
            Assert.Equal(2, data.Single(d => d.EnrollmentDate == new DateTime(2012, 9, 1)).StudentCount);
        }

        [Fact]
        public async Task About_EmptyDatabase_ReturnsEmptyList()
        {
            using var ctx = TestDb.CreateInMemory();
            var result = await Create(ctx).About();
            Assert.Empty(ControllerTestHelper.ModelOf<IEnumerable<EnrollmentDataGroup>>(result));
        }

        [Fact]
        public void Contact_SetsMessage()
        {
            using var ctx = TestDb.CreateInMemory();
            var controller = Create(ctx);
            var result = controller.Contact();
            Assert.IsType<ViewResult>(result);
            Assert.Equal("Your contact page.", controller.ViewBag.Message);
        }

        [Fact]
        public void Error_WithoutActivity_UsesTraceIdentifier()
        {
            using var ctx = TestDb.CreateInMemory();
            Activity.Current = null;
            var result = Create(ctx).Error();
            var vm = ControllerTestHelper.ModelOf<ErrorViewModel>(result);
            Assert.Equal("test-trace-id", vm.RequestId);
            Assert.True(vm.ShowRequestId);
        }

        [Fact]
        public void Error_WithActivity_UsesActivityId()
        {
            using var ctx = TestDb.CreateInMemory();
            using var activity = new Activity("test");
            activity.Start();
            try
            {
                var result = Create(ctx).Error();
                var vm = ControllerTestHelper.ModelOf<ErrorViewModel>(result);
                Assert.Equal(activity.Id, vm.RequestId);
            }
            finally
            {
                activity.Stop();
            }
        }
    }
}
