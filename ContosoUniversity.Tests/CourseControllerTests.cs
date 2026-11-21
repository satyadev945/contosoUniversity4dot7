using ContosoUniversity.Controllers;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContosoUniversity.Tests
{
    public class CourseControllerTests
    {
        private static SchoolContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new SchoolContext(options);
        }

        [Fact]
        public async Task Details_NullId_ReturnsBadRequest()
        {
            using var context = CreateContext();
            var controller = new CourseController(context, NullLogger<CourseController>.Instance);

            var result = await controller.Details(null);

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Details_UnknownId_ReturnsNotFound()
        {
            using var context = CreateContext();
            var controller = new CourseController(context, NullLogger<CourseController>.Instance);

            var result = await controller.Details(9999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ExistingCourse_ReturnsViewWithDepartment()
        {
            using var context = CreateContext();
            context.Departments.Add(new Department { DepartmentID = 1, Name = "English", Budget = 1000, StartDate = DateTime.Today });
            context.Courses.Add(new Course { CourseID = 2021, Title = "Composition", Credits = 3, DepartmentID = 1 });
            await context.SaveChangesAsync();
            var controller = new CourseController(context, NullLogger<CourseController>.Instance);

            var result = await controller.Details(2021);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Course>(view.Model);
            Assert.Equal("English", model.Department.Name);
        }

        [Fact]
        public async Task Index_ReturnsAllCourses()
        {
            using var context = CreateContext();
            context.Departments.Add(new Department { DepartmentID = 1, Name = "English", Budget = 1000, StartDate = DateTime.Today });
            context.Courses.Add(new Course { CourseID = 2021, Title = "Composition", Credits = 3, DepartmentID = 1 });
            context.Courses.Add(new Course { CourseID = 2042, Title = "Literature", Credits = 4, DepartmentID = 1 });
            await context.SaveChangesAsync();
            var controller = new CourseController(context, NullLogger<CourseController>.Instance);

            var result = await controller.Index();

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Course>>(view.Model);
            Assert.Equal(2, model.Count());
        }
    }
}
