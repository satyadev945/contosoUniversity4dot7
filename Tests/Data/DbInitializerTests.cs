using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.Data
{
    public class DbInitializerTests
    {
        [Fact]
        public async Task InitializeAsync_SeedsEmptyDatabase()
        {
            using var db = new TestDb().MarkMigrationsApplied();
            using (var ctx = db.CreateContext())
            {
                await DbInitializer.InitializeAsync(ctx);
            }
            using var verify = db.CreateContext();
            Assert.Equal(8, await verify.Students.CountAsync());
            Assert.Equal(5, await verify.Instructors.CountAsync());
            Assert.Equal(4, await verify.Departments.CountAsync());
            Assert.Equal(7, await verify.Courses.CountAsync());
            Assert.Equal(3, await verify.OfficeAssignments.CountAsync());
            Assert.Equal(11, await verify.Enrollments.CountAsync());
        }

        [Fact]
        public async Task InitializeAsync_AssignsInstructorsAndDepartments()
        {
            using var db = new TestDb().MarkMigrationsApplied();
            using (var ctx = db.CreateContext())
            {
                await DbInitializer.InitializeAsync(ctx);
            }
            using var verify = db.CreateContext();
            var chemistry = await verify.Courses.Include(c => c.Instructors).Include(c => c.Department).SingleAsync(c => c.Title == "Chemistry");
            Assert.Equal(2, chemistry.Instructors.Count);
            Assert.Equal("Engineering", chemistry.Department.Name);
            var english = await verify.Departments.Include(d => d.Administrator).SingleAsync(d => d.Name == "English");
            Assert.Equal("Abercrombie", english.Administrator.LastName);
        }

        [Fact]
        public async Task InitializeAsync_IsIdempotent()
        {
            using var db = new TestDb().MarkMigrationsApplied();
            using (var ctx = db.CreateContext())
            {
                await DbInitializer.InitializeAsync(ctx);
                await DbInitializer.InitializeAsync(ctx);
            }
            using var verify = db.CreateContext();
            Assert.Equal(8, await verify.Students.CountAsync());
        }

        [Fact]
        public async Task InitializeAsync_ExistingStudents_SkipsSeeding()
        {
            using var db = new TestDb().MarkMigrationsApplied();
            using (var ctx = db.CreateContext())
            {
                ctx.Students.Add(new Student { LastName = "Only", FirstMidName = "One", EnrollmentDate = DateTime.Today });
                await ctx.SaveChangesAsync();
                await DbInitializer.InitializeAsync(ctx);
            }
            using var verify = db.CreateContext();
            Assert.Equal(1, await verify.Students.CountAsync());
            Assert.Empty(await verify.Courses.ToListAsync());
        }
    }
}
