using ContosoUniversity.Controllers;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContosoUniversity.Tests
{
    public class ControllerTests
    {
        private static async Task<TestDatabase> CreateSeededDatabaseAsync()
        {
            var db = new TestDatabase();
            using var context = db.CreateContext();
            await SchoolInitializer.SeedAsync(context);
            return db;
        }

        [Fact]
        public async Task Seeder_IsIdempotent()
        {
            using var db = await CreateSeededDatabaseAsync();
            using var context = db.CreateContext();
            var before = context.Students.Count();

            await SchoolInitializer.SeedAsync(context);

            Assert.Equal(8, before);
            Assert.Equal(before, context.Students.Count());
        }

        [Fact]
        public async Task StudentsIndex_PagesAndKeepsSortOrder()
        {
            using var db = await CreateSeededDatabaseAsync();
            using var context = db.CreateContext();
            var controller = new StudentsController(context, NullLogger<StudentsController>.Instance);

            var result = await controller.Index("name_desc", null, null, 2);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PaginatedList<Student>>(view.Model);
            Assert.Equal(3, model.Count);
            Assert.Equal(3, model.PageCount);
            Assert.Equal(2, model.PageNumber);
            Assert.Equal("name_desc", controller.ViewBag.CurrentSort);
            Assert.True(model[0].LastName.CompareTo(model[1].LastName) >= 0);
        }

        [Fact]
        public async Task StudentsIndex_FiltersBySearchString()
        {
            using var db = await CreateSeededDatabaseAsync();
            using var context = db.CreateContext();
            var controller = new StudentsController(context, NullLogger<StudentsController>.Instance);

            var result = await controller.Index(null, null, "Alonso", null);

            var model = Assert.IsType<PaginatedList<Student>>(Assert.IsType<ViewResult>(result).Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task StudentsDetails_ReturnsBadRequestAndNotFound()
        {
            using var db = await CreateSeededDatabaseAsync();
            using var context = db.CreateContext();
            var controller = new StudentsController(context, NullLogger<StudentsController>.Instance);

            Assert.IsType<BadRequestResult>(await controller.Details(null));
            Assert.IsType<NotFoundResult>(await controller.Details(9999));
        }

        [Fact]
        public async Task CourseDelete_RemovesCourseAndHandlesMissing()
        {
            using var db = await CreateSeededDatabaseAsync();
            using var context = db.CreateContext();
            var controller = new CourseController(context, NullLogger<CourseController>.Instance);

            var result = await controller.DeleteConfirmed(2042);
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Null(await context.Courses.FindAsync(2042));

            Assert.IsType<NotFoundResult>(await controller.DeleteConfirmed(2042));
        }

        [Fact]
        public async Task InstructorIndex_LoadsCoursesAndEnrollments()
        {
            using var db = await CreateSeededDatabaseAsync();
            using var context = db.CreateContext();
            var instructorId = context.Instructors.Single(i => i.LastName == "Kapoor").ID;
            var controller = new InstructorController(context);

            var result = await controller.Index(instructorId, 1050);

            var model = Assert.IsType<InstructorIndexData>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(5, model.Instructors.Count());
            Assert.Single(model.Courses);
            Assert.Equal(3, model.Enrollments.Count());
            Assert.All(model.Courses, c => Assert.NotNull(c.Department));
        }

        [Fact]
        public async Task HomeAbout_GroupsStudentsByEnrollmentDate()
        {
            using var db = await CreateSeededDatabaseAsync();
            using var context = db.CreateContext();
            var controller = new HomeController(context);

            var result = await controller.About();

            var model = Assert.IsAssignableFrom<IEnumerable<EnrollmentDataGroup>>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(8, model.Sum(g => g.StudentCount));
        }
    }
}
