using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.DAL
{
    public class SchoolContextTests
    {
        [Fact]
        public void Constructor_ExposesAllDbSets()
        {
            using var ctx = TestDb.CreateInMemory();
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
        public void Model_UsesSingularTableNames(Type entityType, string tableName)
        {
            using var conn = TestDb.OpenSqlite();
            using var ctx = TestDb.CreateSqlite(conn);
            var entity = ctx.Model.FindEntityType(entityType);
            Assert.NotNull(entity);
            Assert.Equal(tableName, entity.GetTableName());
        }

        [Fact]
        public void Model_DepartmentBudget_UsesMoneyColumn()
        {
            using var conn = TestDb.OpenSqlite();
            using var ctx = TestDb.CreateSqlite(conn);
            var prop = ctx.Model.FindEntityType(typeof(Department)).FindProperty("Budget");
            Assert.Equal("money", prop.GetColumnType());
        }

        [Fact]
        public void Model_DepartmentAdministrator_IsNoAction()
        {
            using var ctx = TestDb.CreateInMemory();
            var fk = ctx.Model.FindEntityType(typeof(Department)).GetForeignKeys()
                .Single(f => f.PrincipalEntityType.ClrType == typeof(Instructor));
            Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior);
        }

        [Fact]
        public void Model_CourseInstructor_JoinTableConfigured()
        {
            using var ctx = TestDb.CreateInMemory();
            var skip = ctx.Model.FindEntityType(typeof(Course)).GetSkipNavigations().Single();
            Assert.Equal("CourseInstructor", skip.JoinEntityType.GetTableName());
            Assert.Equal(2, skip.JoinEntityType.FindPrimaryKey().Properties.Count);
        }

        [Fact]
        public async Task SaveAndQuery_InMemory_Works()
        {
            using var ctx = TestDb.CreateInMemory();
            ctx.Students.Add(new Student { FirstMidName = "A", LastName = "B", EnrollmentDate = DateTime.Today });
            await ctx.SaveChangesAsync();
            Assert.Equal(1, await ctx.Students.CountAsync());
        }

        [Fact]
        public async Task SaveAndQuery_Sqlite_CreatesSchemaAndPersists()
        {
            using var conn = TestDb.OpenSqlite();
            using var ctx = TestDb.CreateSqlite(conn);
            await ctx.Database.EnsureCreatedAsync();
            var i = new Instructor { FirstMidName = "K", LastName = "A", HireDate = DateTime.Today };
            ctx.Instructors.Add(i);
            await ctx.SaveChangesAsync();
            ctx.Departments.Add(new Department { Name = "English", Budget = 10, StartDate = DateTime.Today, InstructorID = i.ID });
            await ctx.SaveChangesAsync();
            ctx.Courses.Add(new Course { CourseID = 1, Title = "Test", Credits = 1, DepartmentID = 1, Instructors = new List<Instructor> { i } });
            await ctx.SaveChangesAsync();
            var course = await ctx.Courses.Include(c => c.Instructors).SingleAsync();
            Assert.Single(course.Instructors);
        }
    }
}
