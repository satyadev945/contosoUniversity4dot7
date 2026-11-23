using ContosoUniversity.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace ContosoUniversity.Migrations
{
    public class MigrationsTests
    {
        [Fact]
        public void InitialCreate_UpOperations_CreateAllTables()
        {
            var m = new InitialCreate();
            var tables = m.UpOperations.OfType<CreateTableOperation>().Select(o => o.Name).ToList();
            Assert.Contains("Student", tables);
            Assert.Contains("Course", tables);
            Assert.Contains("Department", tables);
            Assert.Contains("Instructor", tables);
            Assert.Contains("Enrollment", tables);
            Assert.Contains("OfficeAssignment", tables);
            Assert.Contains("CourseInstructor", tables);
        }

        [Fact]
        public void InitialCreate_DownOperations_DropAllTables()
        {
            var m = new InitialCreate();
            var drops = m.DownOperations.OfType<DropTableOperation>().Select(o => o.Name).ToList();
            Assert.Equal(7, drops.Count);
        }

        [Fact]
        public void InitialCreate_TargetModel_ContainsEntities()
        {
            var m = new InitialCreate();
            Assert.NotNull(m.TargetModel);
        }

        [Fact]
        public void InitialCreate_HasMigrationId()
        {
            var attr = typeof(InitialCreate).GetCustomAttributes(typeof(MigrationAttribute), false);
            Assert.Single(attr);
            Assert.Equal("20261121153722_InitialCreate", ((MigrationAttribute)attr[0]).Id);
        }

        [Fact]
        public void Snapshot_ModelContainsEntities()
        {
            var type = typeof(InitialCreate).Assembly.GetType("ContosoUniversity.Migrations.SchoolContextModelSnapshot")!;
            var snapshot = (ModelSnapshot)Activator.CreateInstance(type)!;
            Assert.NotNull(snapshot.Model);
            Assert.True(snapshot.Model.GetEntityTypes().Count() >= 6);
        }
    }
}
