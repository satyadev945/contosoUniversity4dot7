using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class InstructorControllerTests
    {
        private static InstructorController Create(SchoolContext ctx) =>
            new InstructorController(ctx, NullLogger<InstructorController>.Instance);

        [Fact]
        public async Task Index_NoId_ReturnsOrderedInstructors()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());

            var result = await Create(ctx).Index(null, null);

            var model = Assert.IsType<InstructorIndexData>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(new[] { "Abercrombie", "Fakhouri" }, model.Instructors.Select(i => i.LastName));
            Assert.Null(model.Courses);
            Assert.Null(model.Enrollments);
        }

        [Fact]
        public async Task Index_WithInstructorId_ReturnsCourses()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);

            var result = await controller.Index(1, null);

            var model = Assert.IsType<InstructorIndexData>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(1, (int)controller.ViewBag.InstructorID);
            Assert.Equal("Chemistry", Assert.Single(model.Courses).Title);
            Assert.Null(model.Enrollments);
        }

        [Fact]
        public async Task Index_WithInstructorAndCourse_ReturnsEnrollments()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);

            var result = await controller.Index(1, 1050);

            var model = Assert.IsType<InstructorIndexData>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(1050, (int)controller.ViewBag.CourseID);
            Assert.Equal(2, model.Enrollments.Count());
            Assert.All(model.Enrollments, e => Assert.NotNull(e.Student));
        }

        [Fact]
        public async Task Index_UnknownInstructor_ReturnsNotFound()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            Assert.IsType<NotFoundResult>(await Create(ctx).Index(999, null));
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
        public async Task Details_Known_ReturnsInstructorWithOffice()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var result = await Create(ctx).Details(1);
            var instructor = Assert.IsType<Instructor>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal("Smith 17", instructor.OfficeAssignment.Location);
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
                var result = await Create(ctx).Create(new Instructor { LastName = "Zheng", FirstMidName = "Roger", HireDate = new DateTime(2004, 2, 12) });
                Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            }
            using var verify = TestDb.Create(name);
            Assert.Equal(3, verify.Instructors.Count());
        }

        [Fact]
        public async Task CreatePost_InvalidModelState_ReturnsView()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);
            controller.ModelState.AddModelError("LastName", "Required");
            var instructor = new Instructor();

            var result = await controller.Create(instructor);

            Assert.Same(instructor, Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(2, ctx.Instructors.Count());
        }

        [Fact]
        public async Task CreatePost_DbUpdateException_AddsModelError()
        {
            using var ctx = TestDb.CreateThrowing(TestDb.CreateSeeded(), new DbUpdateException("boom"));
            var controller = Create(ctx);

            var result = await controller.Create(new Instructor { LastName = "A", FirstMidName = "B" });

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState[""].Errors, e => e.ErrorMessage.Contains("Unable to save changes"));
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
        public async Task EditGet_Known_ReturnsInstructor()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var result = await Create(ctx).Edit(2);
            Assert.Equal("Fakhouri", Assert.IsType<Instructor>(Assert.IsType<ViewResult>(result).Model).LastName);
        }

        [Fact]
        public async Task EditPost_Valid_UpdatesAndRedirects()
        {
            var name = TestDb.CreateSeeded();
            using (var ctx = TestDb.Create(name))
            {
                var result = await Create(ctx).Edit(new Instructor { ID = 2, LastName = "Updated", FirstMidName = "Fadi", HireDate = new DateTime(2000, 1, 1) });
                Assert.IsType<RedirectToActionResult>(result);
            }
            using var verify = TestDb.Create(name);
            Assert.Equal("Updated", verify.Instructors.Single(i => i.ID == 2).LastName);
        }

        [Fact]
        public async Task EditPost_InvalidModelState_ReturnsView()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var controller = Create(ctx);
            controller.ModelState.AddModelError("LastName", "Required");
            Assert.IsType<ViewResult>(await controller.Edit(new Instructor { ID = 2 }));
        }

        [Fact]
        public async Task EditPost_DbUpdateException_AddsModelError()
        {
            using var ctx = TestDb.CreateThrowing(TestDb.CreateSeeded(), new DbUpdateException("boom"));
            var controller = Create(ctx);

            var result = await controller.Edit(new Instructor { ID = 2, LastName = "A", FirstMidName = "B" });

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState[""].Errors, e => e.ErrorMessage.Contains("Unable to save changes"));
        }

        [Fact]
        public async Task EditPost_ConcurrencyAndMissing_ReturnsNotFound()
        {
            using var ctx = TestDb.CreateThrowing(TestDb.CreateSeeded(), new DbUpdateConcurrencyException("gone"));
            var result = await Create(ctx).Edit(new Instructor { ID = 999, LastName = "A", FirstMidName = "B" });
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditPost_ConcurrencyAndExisting_Rethrows()
        {
            using var ctx = TestDb.CreateThrowing(TestDb.CreateSeeded(), new DbUpdateConcurrencyException("conflict"));
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                Create(ctx).Edit(new Instructor { ID = 2, LastName = "A", FirstMidName = "B" }));
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
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete(999));
        }

        [Fact]
        public async Task DeleteGet_Known_ReturnsInstructor()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var result = await Create(ctx).Delete(1);
            Assert.Equal(1, Assert.IsType<Instructor>(Assert.IsType<ViewResult>(result).Model).ID);
        }

        [Fact]
        public async Task DeleteConfirmed_Existing_RemovesInstructorAndClearsDepartmentAdministrator()
        {
            var name = TestDb.CreateSeeded();
            using (var ctx = TestDb.Create(name))
            {
                var result = await Create(ctx).DeleteConfirmed(1);
                Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            }
            using var verify = TestDb.Create(name);
            Assert.Null(verify.Instructors.Find(1));
            Assert.Null(verify.Departments.Single(d => d.DepartmentID == 1).InstructorID);
            Assert.Empty(verify.OfficeAssignments);
        }

        [Fact]
        public async Task DeleteConfirmed_Missing_RedirectsWithoutChanges()
        {
            using var ctx = TestDb.Create(TestDb.CreateSeeded());
            var result = await Create(ctx).DeleteConfirmed(999);
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(2, ctx.Instructors.Count());
        }
    }
}
