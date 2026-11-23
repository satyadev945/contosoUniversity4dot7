using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.DAL
{
    public class DbInitializerTests
    {
        [Fact]
        public async Task InitializeAsync_InMemoryProvider_ThrowsBecauseMigrationsNeedRelational()
        {
            using var ctx = TestDb.CreateInMemory();
            await Assert.ThrowsAsync<InvalidOperationException>(() => DbInitializer.InitializeAsync(ctx));
        }

        [Fact]
        public async Task InitializeAsync_EmptyDatabase_SeedsAllData()
        {
            using var conn = TestDb.OpenSqlite();
            using var ctx = TestDb.CreateSqlite(conn);

            await DbInitializer.InitializeAsync(ctx);

            Assert.Equal(8, await ctx.Students.CountAsync());
            Assert.Equal(5, await ctx.Instructors.CountAsync());
            Assert.Equal(4, await ctx.Departments.CountAsync());
            Assert.Equal(7, await ctx.Courses.CountAsync());
            Assert.Equal(3, await ctx.OfficeAssignments.CountAsync());
            Assert.Equal(11, await ctx.Enrollments.CountAsync());
        }

        [Fact]
        public async Task InitializeAsync_SeedsCourseInstructorRelations()
        {
            using var conn = TestDb.OpenSqlite();
            using var ctx = TestDb.CreateSqlite(conn);
            await DbInitializer.InitializeAsync(ctx);

            var chem = await ctx.Courses.Include(c => c.Instructors).SingleAsync(c => c.CourseID == 1050);
            Assert.Equal(2, chem.Instructors.Count);
            Assert.Null((await ctx.Enrollments.SingleAsync(e => e.StudentID == ctx.Students.Single(s => s.LastName == "Anand").ID && e.CourseID == 1050)).Grade);
        }

        [Fact]
        public async Task InitializeAsync_CalledTwice_DoesNotDuplicate()
        {
            using var conn = TestDb.OpenSqlite();
            using var ctx = TestDb.CreateSqlite(conn);

            await DbInitializer.InitializeAsync(ctx);
            await DbInitializer.InitializeAsync(ctx);

            Assert.Equal(8, await ctx.Students.CountAsync());
            Assert.Equal(5, await ctx.Instructors.CountAsync());
        }

        [Fact]
        public async Task InitializeAsync_ExistingStudents_SkipsSeeding()
        {
            using var conn = TestDb.OpenSqlite();
            using var ctx = TestDb.CreateSqlite(conn);
            await ctx.Database.MigrateAsync();
            ctx.Students.Add(new Student { FirstMidName = "Only", LastName = "One", EnrollmentDate = DateTime.Today });
            await ctx.SaveChangesAsync();

            await DbInitializer.InitializeAsync(ctx);

            Assert.Equal(1, await ctx.Students.CountAsync());
            Assert.Equal(0, await ctx.Instructors.CountAsync());
        }
    }
}
