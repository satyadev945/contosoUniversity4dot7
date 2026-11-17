using ContosoUniversity.Controllers;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoUniversity.Tests
{
    public class CourseControllerTests
    {
        private static SchoolContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new SchoolContext(options);
            context.Departments.Add(new Department { DepartmentID = 1, Name = "English", Budget = 1000, StartDate = DateTime.Today });
            context.Courses.Add(new Course { CourseID = 1050, Title = "Chemistry", Credits = 3, DepartmentID = 1 });
            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task Details_WithoutId_ReturnsBadRequest()
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
        public async Task Index_ReturnsAllCourses()
        {
            using var context = CreateContext();
            var controller = new CourseController(context, NullLogger<CourseController>.Instance);

            var result = await controller.Index();

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Course>>(view.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task DbSeeder_IsIdempotent()
        {
            using var context = CreateContext();
            context.Courses.RemoveRange(context.Courses);
            context.Departments.RemoveRange(context.Departments);
            await context.SaveChangesAsync();

            await DbSeeder.SeedAsync(context);
            var studentCount = await context.Students.CountAsync();
            await DbSeeder.SeedAsync(context);

            Assert.Equal(studentCount, await context.Students.CountAsync());
            Assert.Equal(7, await context.Courses.CountAsync());
        }
    }
}
