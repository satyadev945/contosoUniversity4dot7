using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class InstructorControllerTests
    {
        private static InstructorController Create(SchoolContext ctx)
        {
            var c = new InstructorController(ctx);
            ControllerTestHelper.Setup(c);
            return c;
        }

        [Fact]
        public async Task Index_NoParams_ReturnsInstructorsOrderedByLastName()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Index(null, null);
            var vm = ControllerTestHelper.ModelOf<InstructorIndexData>(result);
            Assert.Equal(new[] { "Abercrombie", "Fakhouri" }, vm.Instructors.Select(i => i.LastName).ToArray());
            Assert.Null(vm.Courses);
            Assert.Null(vm.Enrollments);
        }

        [Fact]
        public async Task Index_WithInstructorId_SetsCourses()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var controller = Create(ctx);
            var result = await controller.Index(1, null);
            var vm = ControllerTestHelper.ModelOf<InstructorIndexData>(result);
            Assert.Equal(2, vm.Courses.Count());
            Assert.Equal(1, controller.ViewBag.InstructorID);
            Assert.Null(vm.Enrollments);
        }

        [Fact]
        public async Task Index_UnknownInstructorId_ReturnsNotFound()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            Assert.IsType<NotFoundResult>(await Create(ctx).Index(99, null));
        }

        [Fact]
        public async Task Index_WithInstructorAndCourse_LoadsEnrollments()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var controller = Create(ctx);
            var result = await controller.Index(1, 2021);
            var vm = ControllerTestHelper.ModelOf<InstructorIndexData>(result);
            Assert.Equal(2, vm.Enrollments.Count());
            Assert.All(vm.Enrollments, e => Assert.NotNull(e.Student));
            Assert.Equal(2021, controller.ViewBag.CourseID);
        }

        [Fact]
        public async Task Index_CourseWithoutInstructor_IgnoresCourse()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Index(null, 2021);
            var vm = ControllerTestHelper.ModelOf<InstructorIndexData>(result);
            Assert.Null(vm.Enrollments);
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
            Assert.IsType<NotFoundResult>(await Create(ctx).Details(5));
        }

        [Fact]
        public async Task Details_Existing_ReturnsInstructorWithOffice()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Details(1);
            var i = ControllerTestHelper.ModelOf<Instructor>(result);
            Assert.Equal("Smith 17", i.OfficeAssignment.Location);
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
            var result = await Create(ctx).Create(new Instructor { ID = 50, LastName = "New", FirstMidName = "Person", HireDate = DateTime.Today });
            Assert.IsType<RedirectToActionResult>(result);
            using var verify = TestDb.CreateInMemory(db);
            Assert.NotNull(await verify.Instructors.FindAsync(50));
        }

        [Fact]
        public async Task Create_Post_Invalid_ReturnsView()
        {
            using var ctx = TestDb.CreateInMemory();
            var controller = Create(ctx);
            controller.ModelState.AddModelError("LastName", "required");
            var model = new Instructor();
            var result = await controller.Create(model);
            Assert.Same(model, ControllerTestHelper.ModelOf<Instructor>(result));
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
            Assert.IsType<NotFoundResult>(await Create(ctx).Edit((int?)77));
        }

        [Fact]
        public async Task Edit_Get_Existing_ReturnsInstructor()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Edit((int?)2);
            Assert.Equal("Fakhouri", ControllerTestHelper.ModelOf<Instructor>(result).LastName);
        }

        [Fact]
        public async Task Edit_Post_Valid_UpdatesAndRedirects()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Edit(new Instructor { ID = 2, LastName = "Changed", FirstMidName = "Fadi", HireDate = new DateTime(2002, 7, 6) });
            Assert.IsType<RedirectToActionResult>(result);
            using var verify = TestDb.CreateInMemory(db);
            Assert.Equal("Changed", (await verify.Instructors.FindAsync(2)).LastName);
        }

        [Fact]
        public async Task Edit_Post_Invalid_ReturnsView()
        {
            using var ctx = TestDb.CreateInMemory();
            var controller = Create(ctx);
            controller.ModelState.AddModelError("LastName", "required");
            Assert.IsType<ViewResult>(await controller.Edit(new Instructor { ID = 2 }));
        }

        [Fact]
        public async Task Delete_Get_NullId_ReturnsBadRequest()
        {
            using var ctx = TestDb.CreateInMemory();
            Assert.IsType<BadRequestResult>(await Create(ctx).Delete(null));
        }

        [Fact]
        public async Task Delete_Get_Unknown_ReturnsNotFound()
        {
            using var ctx = TestDb.CreateInMemory();
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete(3));
        }

        [Fact]
        public async Task Delete_Get_Existing_ReturnsInstructor()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).Delete(1);
            Assert.Equal(1, ControllerTestHelper.ModelOf<Instructor>(result).ID);
        }

        [Fact]
        public async Task DeleteConfirmed_Existing_RemovesAndRedirects()
        {
            var db = TestDb.SeedBasic();
            using var ctx = TestDb.CreateInMemory(db);
            var result = await Create(ctx).DeleteConfirmed(2);
            Assert.IsType<RedirectToActionResult>(result);
            using var verify = TestDb.CreateInMemory(db);
            Assert.Null(await verify.Instructors.FindAsync(2));
        }

        [Fact]
        public async Task DeleteConfirmed_Unknown_StillRedirects()
        {
            using var ctx = TestDb.CreateInMemory();
            Assert.IsType<RedirectToActionResult>(await Create(ctx).DeleteConfirmed(123));
        }
    }
}
