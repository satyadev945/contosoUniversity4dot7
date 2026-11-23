using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.Data
{
    public class SchoolContextTests
    {
        [Theory]
        [InlineData(typeof(Course), "Course")]
        [InlineData(typeof(Department), "Department")]
        [InlineData(typeof(Enrollment), "Enrollment")]
        [InlineData(typeof(Instructor), "Instructor")]
        [InlineData(typeof(Student), "Student")]
        [InlineData(typeof(OfficeAssignment), "OfficeAssignment")]
        public void Model_UsesSingularTableNames(Type entity, string table)
        {
            using var db = new TestDb();
            using var ctx = db.CreateContext();
            Assert.Equal(table, ctx.Model.FindEntityType(entity)!.GetTableName());
        }

        [Fact]
        public void Model_HasCourseInstructorJoinTable()
        {
            using var db = new TestDb();
            using var ctx = db.CreateContext();
            var join = ctx.Model.GetEntityTypes().Single(e => e.GetTableName() == "CourseInstructor");
            Assert.Equal(2, join.FindPrimaryKey()!.Properties.Count);
        }

        [Fact]
        public void Model_DepartmentAdministratorIsOptionalClientSetNull()
        {
            using var db = new TestDb();
            using var ctx = db.CreateContext();
            var fk = ctx.Model.FindEntityType(typeof(Department))!
                .GetForeignKeys().Single(f => f.PrincipalEntityType.ClrType == typeof(Instructor));
            Assert.False(fk.IsRequired);
            Assert.Equal(DeleteBehavior.ClientSetNull, fk.DeleteBehavior);
        }

        [Fact]
        public void Model_OfficeAssignmentIsOneToOne()
        {
            using var db = new TestDb();
            using var ctx = db.CreateContext();
            var fk = ctx.Model.FindEntityType(typeof(OfficeAssignment))!.GetForeignKeys().Single();
            Assert.True(fk.IsUnique);
        }

        [Fact]
        public void DbSets_AreExposed()
        {
            using var db = new TestDb();
            using var ctx = db.CreateContext();
            Assert.NotNull(ctx.Courses);
            Assert.NotNull(ctx.Departments);
            Assert.NotNull(ctx.Enrollments);
            Assert.NotNull(ctx.Instructors);
            Assert.NotNull(ctx.Students);
            Assert.NotNull(ctx.OfficeAssignments);
        }

        [Fact]
        public async Task SeedAndQuery_RoundTrips()
        {
            using var db = new TestDb();
            db.Seed();
            using var ctx = db.CreateContext();
            var course = await ctx.Courses.Include(c => c.Instructors).SingleAsync(c => c.CourseID == 1050);
            Assert.Equal(2, course.Instructors.Count);
            Assert.Equal(5, await ctx.Students.CountAsync());
        }

        [Fact]
        public async Task DeletingInstructor_CascadesToOfficeAssignment()
        {
            using var db = new TestDb();
            db.Seed();
            using (var ctx = db.CreateContext())
            {
                var dept = await ctx.Departments.Where(d => d.InstructorID == 1).ToListAsync();
                dept.ForEach(d => d.InstructorID = null);
                ctx.Instructors.Remove(await ctx.Instructors.SingleAsync(i => i.ID == 1));
                await ctx.SaveChangesAsync();
            }
            using var verify = db.CreateContext();
            Assert.Empty(verify.OfficeAssignments);
        }
    }
}
