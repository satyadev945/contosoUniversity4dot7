using System;
using System.Linq;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace ContosoUniversity.Data
{
    public class SchoolContextTests
    {
        [Fact]
        public void Constructor_ExposesAllDbSets()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
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
        public void OnModelCreating_UsesSingularTableNames(Type entity, string table)
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            Assert.Equal(table, ctx.Model.FindEntityType(entity).GetTableName());
        }

        [Fact]
        public void OnModelCreating_ConfiguresCourseInstructorJoinTable()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            var join = ctx.Model.GetEntityTypes().Single(t => t.GetTableName() == "CourseInstructor");
            var key = join.FindPrimaryKey();
            Assert.Equal(new[] { "CourseID", "InstructorID" }, key.Properties.Select(p => p.Name).OrderBy(n => n));
        }

        [Fact]
        public void OnModelCreating_DepartmentAdministrator_UsesClientSetNull()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            var fk = ctx.Model.FindEntityType(typeof(Department))
                .GetForeignKeys().Single(f => f.PrincipalEntityType.ClrType == typeof(Instructor));
            Assert.Equal(DeleteBehavior.ClientSetNull, fk.DeleteBehavior);
            Assert.Equal("InstructorID", fk.Properties.Single().Name);
        }

        [Fact]
        public void CourseID_IsNotDatabaseGenerated()
        {
            using var ctx = TestDb.Create(TestDb.NewName());
            var prop = ctx.Model.FindEntityType(typeof(Course)).FindProperty(nameof(Course.CourseID));
            Assert.Equal(ValueGenerated.Never, prop.ValueGenerated);
        }

        [Fact]
        public void FirstMidName_MapsToFirstNameColumn()
        {
            var attr = typeof(Student).GetProperty(nameof(Student.FirstMidName))
                .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.ColumnAttribute), false)
                .Cast<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>().Single();
            Assert.Equal("FirstName", attr.Name);
            using var ctx = TestDb.Create(TestDb.NewName());
            Assert.NotNull(ctx.Model.FindEntityType(typeof(Student)).FindProperty(nameof(Student.FirstMidName)));
        }

        [Fact]
        public void SaveAndQuery_RoundTripsSeededGraph()
        {
            var name = TestDb.CreateSeeded();
            using var ctx = TestDb.Create(name);
            Assert.Equal(2, ctx.Instructors.Count());
            Assert.Equal(2, ctx.Departments.Count());
            Assert.Equal(2, ctx.Courses.Count());
            Assert.Equal(5, ctx.Students.Count());
            Assert.Equal(3, ctx.Enrollments.Count());
            Assert.Equal(1, ctx.OfficeAssignments.Count());
            var course = ctx.Courses.Include(c => c.Instructors).Single(c => c.CourseID == 1050);
            Assert.Single(course.Instructors);
        }
    }
}
