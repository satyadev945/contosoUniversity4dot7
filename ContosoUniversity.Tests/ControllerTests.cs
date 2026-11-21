using ContosoUniversity.Controllers;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoUniversity.Tests
{
    public class ControllerTests
    {
        [Fact]
        public async Task Course_Details_WithoutId_ReturnsBadRequest()
        {
            using var db = new TestDatabase();
            var controller = new CourseController(db.Context, NullLogger<CourseController>.Instance);

            var result = await controller.Details(null);

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Course_Details_UnknownId_ReturnsNotFound()
        {
            using var db = new TestDatabase();
            var controller = new CourseController(db.Context, NullLogger<CourseController>.Instance);

            var result = await controller.Details(9999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Students_Index_ReturnsPagedResultSortedByLastName()
        {
            using var db = new TestDatabase();
            db.Context.Students.AddRange(
                new Student { LastName = "Zed", FirstMidName = "A", EnrollmentDate = new DateTime(2020, 9, 1) },
                new Student { LastName = "Abe", FirstMidName = "B", EnrollmentDate = new DateTime(2021, 9, 1) });
            await db.Context.SaveChangesAsync();
            var controller = new StudentsController(db.Context, NullLogger<StudentsController>.Instance);

            var result = await controller.Index(null, null, null, null);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Student>>(view.Model);
            Assert.Equal(new[] { "Abe", "Zed" }, model.Select(s => s.LastName).ToArray());
        }

        [Fact]
        public async Task Home_About_GroupsStudentsByEnrollmentDate()
        {
            using var db = new TestDatabase();
            var date = new DateTime(2020, 9, 1);
            db.Context.Students.AddRange(
                new Student { LastName = "A", FirstMidName = "A", EnrollmentDate = date },
                new Student { LastName = "B", FirstMidName = "B", EnrollmentDate = date });
            await db.Context.SaveChangesAsync();
            var controller = new HomeController(db.Context, NullLogger<HomeController>.Instance);

            var result = await controller.About();

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<EnrollmentDataGroup>>(view.Model);
            var group = Assert.Single(model);
            Assert.Equal(2, group.StudentCount);
        }

        [Fact]
        public async Task Students_Delete_RemovesStudent()
        {
            using var db = new TestDatabase();
            var student = new Student { LastName = "A", FirstMidName = "A", EnrollmentDate = DateTime.Today };
            db.Context.Students.Add(student);
            await db.Context.SaveChangesAsync();
            var controller = new StudentsController(db.Context, NullLogger<StudentsController>.Instance);

            var result = await controller.Delete(student.ID);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Empty(db.Context.Students);
        }
    }
}
