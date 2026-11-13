using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Tests;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.DAL
{
    public class DbInitializerTests
    {
        [Fact]
        public async Task SeedAsync_PopulatesAllEntities()
        {
            using var ctx = TestHelpers.CreateContext();
            await DbInitializer.SeedAsync(ctx);

            Assert.Equal(8, await ctx.Students.CountAsync());
            Assert.Equal(5, await ctx.Instructors.CountAsync());
            Assert.Equal(4, await ctx.Departments.CountAsync());
            Assert.Equal(7, await ctx.Courses.CountAsync());
            Assert.Equal(3, await ctx.OfficeAssignments.CountAsync());
            Assert.Equal(11, await ctx.Enrollments.CountAsync());
        }

        [Fact]
        public async Task SeedAsync_IsIdempotent()
        {
            using var ctx = TestHelpers.CreateContext();
            await DbInitializer.SeedAsync(ctx);
            await DbInitializer.SeedAsync(ctx);

            Assert.Equal(8, await ctx.Students.CountAsync());
            Assert.Equal(5, await ctx.Instructors.CountAsync());
            Assert.Equal(4, await ctx.Departments.CountAsync());
            Assert.Equal(7, await ctx.Courses.CountAsync());
            Assert.Equal(3, await ctx.OfficeAssignments.CountAsync());
            Assert.Equal(11, await ctx.Enrollments.CountAsync());
        }

        [Fact]
        public async Task SeedAsync_AssignsInstructorsToCourses()
        {
            using var ctx = TestHelpers.CreateContext();
            await DbInitializer.SeedAsync(ctx);

            var chemistry = await ctx.Courses.Include(c => c.Instructors).SingleAsync(c => c.Title == "Chemistry");
            Assert.Equal(2, chemistry.Instructors.Count);
            Assert.Contains(chemistry.Instructors, i => i.LastName == "Kapoor");
            Assert.Contains(chemistry.Instructors, i => i.LastName == "Harui");
        }

        [Fact]
        public async Task SeedAsync_SetsDepartmentAdministrators()
        {
            using var ctx = TestHelpers.CreateContext();
            await DbInitializer.SeedAsync(ctx);

            var english = await ctx.Departments.SingleAsync(d => d.Name == "English");
            var admin = await ctx.Instructors.SingleAsync(i => i.LastName == "Abercrombie");
            Assert.Equal(admin.ID, english.InstructorID);
            Assert.Equal(350000m, english.Budget);
        }

        [Fact]
        public async Task SeedAsync_UsesExistingRecordsWhenPresent()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.Students.Add(new Student { LastName = "Alexander", FirstMidName = "Existing", EnrollmentDate = DateTime.Today });
            await ctx.SaveChangesAsync();

            await DbInitializer.SeedAsync(ctx);

            var alexander = await ctx.Students.SingleAsync(s => s.LastName == "Alexander");
            Assert.Equal("Existing", alexander.FirstMidName);
            Assert.Equal(8, await ctx.Students.CountAsync());
        }

        [Fact]
        public async Task SeedAsync_CreatesEnrollmentsWithGrades()
        {
            using var ctx = TestHelpers.CreateContext();
            await DbInitializer.SeedAsync(ctx);

            Assert.Contains(await ctx.Enrollments.ToListAsync(), e => e.Grade == Grade.A);
            Assert.Contains(await ctx.Enrollments.ToListAsync(), e => e.Grade == null);
        }
    }
}
