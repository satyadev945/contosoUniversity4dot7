using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.DAL
{
    public class SchoolContextTests
    {
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
            Assert.Equal(table, ctx.Model.FindEntityType(entity).GetTableName());
        }

        [Fact]
        public void Model_ConfiguresCourseInstructorJoinTable()
        {
            using var db = new TestDb();
            using var ctx = db.CreateContext();
            var join = ctx.Model.GetEntityTypes().Single(e => e.GetTableName() == "CourseInstructor");
            Assert.Equal(2, join.FindPrimaryKey().Properties.Count);
        }

        [Fact]
        public void Model_DepartmentAdministratorFk_IsClientSetNull()
        {
            using var db = new TestDb();
            using var ctx = db.CreateContext();
            var fk = ctx.Model.FindEntityType(typeof(Department)).GetForeignKeys()
                .Single(f => f.PrincipalEntityType.ClrType == typeof(Instructor));
            Assert.Equal(DeleteBehavior.ClientSetNull, fk.DeleteBehavior);
        }

        [Fact]
        public async Task SeededData_CanBeQueriedWithRelationships()
        {
            using var db = new TestDb().Seed();
            using var ctx = db.CreateContext();
            var course = await ctx.Courses.Include(c => c.Instructors).Include(c => c.Department)
                .SingleAsync(c => c.CourseID == 2021);
            Assert.Single(course.Instructors);
            Assert.Equal("English", course.Department.Name);
        }

        [Fact]
        public async Task DeletingInstructor_CascadesOfficeAssignment()
        {
            using var db = new TestDb().Seed();
            using (var ctx = db.CreateContext())
            {
                var instr = await ctx.Instructors.Include(i => i.OfficeAssignment).SingleAsync(i => i.LastName == "Fakhouri");
                var dept = await ctx.Departments.SingleAsync(d => d.InstructorID == instr.ID);
                dept.InstructorID = null;
                ctx.Instructors.Remove(instr);
                await ctx.SaveChangesAsync();
            }
            using var verify = db.CreateContext();
            Assert.Empty(await verify.OfficeAssignments.ToListAsync());
        }
    }
}
