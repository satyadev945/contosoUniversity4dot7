using System.Reflection;
using ContosoUniversity.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace ContosoUniversity.Migrations
{
    public class InitialBaselineTests
    {
        private static MigrationBuilder Run(string method)
        {
            var migration = new InitialBaseline();
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
            var m = typeof(InitialBaseline).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotNull(m);
            m.Invoke(migration, new object[] { builder });
            return builder;
        }

        [Fact]
        public void Up_CreatesAllTables()
        {
            var builder = Run("Up");
            var tables = builder.Operations.OfType<CreateTableOperation>().Select(o => o.Name).ToList();
            Assert.Contains("Instructor", tables);
            Assert.Contains("Student", tables);
            Assert.Contains("Department", tables);
            Assert.Contains("Course", tables);
            Assert.Contains("Enrollment", tables);
            Assert.Contains("OfficeAssignment", tables);
            Assert.Contains("CourseInstructor", tables);
        }

        [Fact]
        public void Up_CreatesIndexes()
        {
            var builder = Run("Up");
            Assert.NotEmpty(builder.Operations.OfType<CreateIndexOperation>());
        }

        [Fact]
        public void Down_DropsAllTables()
        {
            var builder = Run("Down");
            var drops = builder.Operations.OfType<DropTableOperation>().Select(o => o.Name).ToList();
            Assert.Equal(7, drops.Count);
            Assert.Contains("Student", drops);
            Assert.Contains("CourseInstructor", drops);
        }

        [Fact]
        public void Migration_HasMigrationAttributeWithId()
        {
            var attr = typeof(InitialBaseline).GetCustomAttribute<MigrationAttribute>();
            Assert.NotNull(attr);
            Assert.Equal("20261113045859_InitialBaseline", attr.Id);
        }

        [Fact]
        public void TargetModel_IsBuilt()
        {
            var migration = new InitialBaseline();
            Assert.NotNull(migration.TargetModel);
            Assert.NotEmpty(migration.TargetModel.GetEntityTypes());
        }
    }
}
