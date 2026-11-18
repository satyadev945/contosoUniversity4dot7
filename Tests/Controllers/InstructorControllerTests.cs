using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class InstructorControllerTests : IDisposable
    {
        private readonly TestDb _db = new TestDb().Seed();

        public void Dispose() => _db.Dispose();

        private InstructorController Create(SchoolContext ctx) =>
            new InstructorController(ctx, NullLogger<InstructorController>.Instance);

        private int IdOf(string lastName)
        {
            using var ctx = _db.CreateContext();
            return ctx.Instructors.Single(i => i.LastName == lastName).ID;
        }

        [Fact]
        public async Task Index_NoParams_ReturnsAllInstructorsOrdered()
        {
            using var ctx = _db.CreateContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Index(null, null));
            var vm = Assert.IsType<InstructorIndexData>(view.Model);
            Assert.Equal(new[] { "Abercrombie", "Fakhouri" }, vm.Instructors.Select(i => i.LastName).ToArray());
            Assert.Null(vm.Courses);
            Assert.Null(vm.Enrollments);
        }

        [Fact]
        public async Task Index_WithInstructorId_ReturnsTheirCourses()
        {
            int id = IdOf("Abercrombie");
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            var view = Assert.IsType<ViewResult>(await controller.Index(id, null));
            var vm = Assert.IsType<InstructorIndexData>(view.Model);
            Assert.Equal(id, controller.ViewData["InstructorID"]);
            Assert.Single(vm.Courses);
            Assert.Equal(2021, vm.Courses.Single().CourseID);
        }

        [Fact]
        public async Task Index_UnknownInstructorId_ReturnsNotFound()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Index(99999, null));
        }

        [Fact]
        public async Task Index_WithInstructorAndCourse_LoadsEnrollmentsAndStudents()
        {
            int id = IdOf("Abercrombie");
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            var view = Assert.IsType<ViewResult>(await controller.Index(id, 2021));
            var vm = Assert.IsType<InstructorIndexData>(view.Model);
            Assert.Equal(2021, controller.ViewData["CourseID"]);
            Assert.Equal(2, vm.Enrollments.Count());
            Assert.All(vm.Enrollments, e => Assert.NotNull(e.Student));
        }

        [Fact]
        public async Task Index_CourseOnlyWithoutInstructor_SearchesAllInstructorCourses()
        {
            using var ctx = _db.CreateContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Index(null, 1045));
            var vm = Assert.IsType<InstructorIndexData>(view.Model);
            Assert.Single(vm.Enrollments);
        }

        [Fact]
        public async Task Index_UnknownCourse_ReturnsNotFound()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Index(null, 99999));
        }

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
        public async Task Details_Existing_ReturnsInstructorWithOffice()
        {
            int id = IdOf("Fakhouri");
            using var ctx = _db.CreateContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Details(id));
            var instr = Assert.IsType<Instructor>(view.Model);
            Assert.Equal("Smith 17", instr.OfficeAssignment.Location);
        }

        [Fact]
        public async Task CreateGet_PopulatesOffices()
        {
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            Assert.IsType<ViewResult>(await controller.Create());
            Assert.IsType<SelectList>(controller.ViewData["ID"]);
        }

        [Fact]
        public async Task CreatePost_Valid_Persists()
        {
            using (var ctx = _db.CreateContext())
            {
                var result = await Create(ctx).Create(new Instructor { LastName = "Zheng", FirstMidName = "Roger", HireDate = new DateTime(2004, 2, 12) });
                Assert.IsType<RedirectToActionResult>(result);
            }
            using var verify = _db.CreateContext();
            Assert.Equal(3, await verify.Instructors.CountAsync());
        }

        [Fact]
        public async Task CreatePost_InvalidModelState_ReturnsView()
        {
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            controller.ModelState.AddModelError("LastName", "Required");
            var instr = new Instructor();
            var view = Assert.IsType<ViewResult>(await controller.Create(instr));
            Assert.Same(instr, view.Model);
        }

        [Fact]
        public async Task CreatePost_SaveFails_AddsModelError()
        {
            using var ctx = _db.CreateThrowingContext();
            var controller = Create(ctx);
            var view = Assert.IsType<ViewResult>(await controller.Create(new Instructor { LastName = "A", FirstMidName = "B" }));
            Assert.False(controller.ModelState.IsValid);
        }

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
        public async Task EditGet_Existing_ReturnsView()
        {
            int id = IdOf("Abercrombie");
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            var view = Assert.IsType<ViewResult>(await controller.Edit((int?)id));
            Assert.Equal(id, Assert.IsType<Instructor>(view.Model).ID);
            Assert.NotNull(controller.ViewData["ID"]);
        }

        [Fact]
        public async Task EditPost_Valid_UpdatesInstructor()
        {
            int id = IdOf("Abercrombie");
            using (var ctx = _db.CreateContext())
            {
                var result = await Create(ctx).Edit(new Instructor { ID = id, LastName = "Renamed", FirstMidName = "Kim", HireDate = new DateTime(1995, 3, 11) });
                Assert.IsType<RedirectToActionResult>(result);
            }
            using var verify = _db.CreateContext();
            Assert.Equal("Renamed", (await verify.Instructors.FindAsync(id)).LastName);
        }

        [Fact]
        public async Task EditPost_InvalidModelState_ReturnsView()
        {
            using var ctx = _db.CreateContext();
            var controller = Create(ctx);
            controller.ModelState.AddModelError("x", "bad");
            Assert.IsType<ViewResult>(await controller.Edit(new Instructor { ID = 1 }));
        }

        [Fact]
        public async Task EditPost_SaveFails_AddsModelError()
        {
            using var ctx = _db.CreateThrowingContext();
            var controller = Create(ctx);
            var view = Assert.IsType<ViewResult>(await controller.Edit(new Instructor { ID = 1, LastName = "A", FirstMidName = "B" }));
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task DeleteGet_NullId_ReturnsBadRequest()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Delete(null));
        }

        [Fact]
        public async Task DeleteGet_Unknown_ReturnsNotFound()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete(99999));
        }

        [Fact]
        public async Task DeleteGet_Existing_ReturnsView()
        {
            int id = IdOf("Fakhouri");
            using var ctx = _db.CreateContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Delete(id));
            Assert.Equal("Fakhouri", Assert.IsType<Instructor>(view.Model).LastName);
        }

        [Fact]
        public async Task DeleteConfirmed_ClearsDepartmentAdministratorAndRemoves()
        {
            int id = IdOf("Fakhouri");
            using (var ctx = _db.CreateContext())
            {
                Assert.IsType<RedirectToActionResult>(await Create(ctx).DeleteConfirmed(id));
            }
            using var verify = _db.CreateContext();
            Assert.Null(await verify.Instructors.FindAsync(id));
            var math = await verify.Departments.SingleAsync(d => d.Name == "Mathematics");
            Assert.Null(math.InstructorID);
        }

        [Fact]
        public async Task DeleteConfirmed_Unknown_StillRedirects()
        {
            using var ctx = _db.CreateContext();
            Assert.IsType<RedirectToActionResult>(await Create(ctx).DeleteConfirmed(99999));
            Assert.Equal(2, await ctx.Instructors.CountAsync());
        }
    }
}
