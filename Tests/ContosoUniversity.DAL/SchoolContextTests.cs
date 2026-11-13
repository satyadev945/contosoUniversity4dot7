using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.DAL
{
    public class SchoolContextTests
    {
        [Fact]
        public void Constructor_CreatesContextWithAllDbSets()
        {
            using var ctx = ContosoUniversity.Tests.TestHelpers.CreateContext();
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
        public void Model_UsesSingularTableNames(Type entityType, string expectedTable)
        {
            using var ctx = new SchoolContext(new DbContextOptionsBuilder<SchoolContext>()
                .UseSqlite("DataSource=:memory:").Options);
            var entity = ctx.Model.FindEntityType(entityType);
            Assert.NotNull(entity);
            Assert.Equal(expectedTable, entity.GetTableName());
        }

        [Fact]
        public void Model_ConfiguresCourseInstructorJoinTable()
        {
            using var ctx = new SchoolContext(new DbContextOptionsBuilder<SchoolContext>()
                .UseSqlite("DataSource=:memory:").Options);
            var join = ctx.Model.GetEntityTypes().Single(e => e.Name == "CourseInstructor");
            Assert.Equal("CourseInstructor", join.GetTableName());
            Assert.Equal(2, join.FindPrimaryKey().Properties.Count);
        }

        [Fact]
        public async Task CanAddAndQueryStudents()
        {
            using var ctx = ContosoUniversity.Tests.TestHelpers.CreateContext();
            ctx.Students.Add(new Student { LastName = "A", FirstMidName = "B", EnrollmentDate = DateTime.Today });
            await ctx.SaveChangesAsync();
            Assert.Equal(1, await ctx.Students.CountAsync());
        }

        [Fact]
        public async Task CourseInstructorManyToMany_Persists()
        {
            using var ctx = ContosoUniversity.Tests.TestHelpers.CreateContext();
            var instructor = new Instructor { LastName = "A", FirstMidName = "B" };
            var course = new Course { CourseID = 1, Title = "Test", Credits = 3, DepartmentID = 1, Instructors = new List<Instructor> { instructor } };
            ctx.Courses.Add(course);
            await ctx.SaveChangesAsync();
            var loaded = await ctx.Courses.Include(c => c.Instructors).SingleAsync();
            Assert.Single(loaded.Instructors);
        }

        [Fact]
        public void Database_CanBeCreatedOnSqlite()
        {
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            conn.Open();
            using var ctx = new SchoolContext(new DbContextOptionsBuilder<SchoolContext>().UseSqlite(conn).Options);
            Assert.True(ctx.Database.EnsureCreated());
            ctx.Students.Add(new Student { LastName = "A", FirstMidName = "B" });
            Assert.Equal(1, ctx.SaveChanges());
        }
    }
}
