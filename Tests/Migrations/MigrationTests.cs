using ContosoUniversity.DAL;
using ContosoUniversity.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace ContosoUniversity.Migrations
{
    public class MigrationTests
    {
        [Fact]
        public void MigrationsAssembly_ContainsInitialCreate()
        {
            using var conn = TestDb.OpenSqlite();
            using var ctx = TestDb.CreateSqlite(conn);
            var assembly = ctx.GetService<IMigrationsAssembly>();
            Assert.Contains(assembly.Migrations, m => m.Key.EndsWith("InitialCreate"));
            Assert.NotNull(assembly.ModelSnapshot);
        }

        [Fact]
        public void Snapshot_ModelContainsAllEntities()
        {
            using var conn = TestDb.OpenSqlite();
            using var ctx = TestDb.CreateSqlite(conn);
            var assembly = ctx.GetService<IMigrationsAssembly>();
            var model = assembly.ModelSnapshot.Model;
            Assert.NotNull(model.FindEntityType("ContosoUniversity.Models.Student"));
            Assert.NotNull(model.FindEntityType("ContosoUniversity.Models.Course"));
            Assert.NotNull(model.FindEntityType("ContosoUniversity.Models.Department"));
            Assert.NotNull(model.FindEntityType("ContosoUniversity.Models.Instructor"));
            Assert.NotNull(model.FindEntityType("ContosoUniversity.Models.Enrollment"));
            Assert.NotNull(model.FindEntityType("ContosoUniversity.Models.OfficeAssignment"));
        }

        [Fact]
        public void Migration_TargetModel_IsBuilt()
        {
            using var conn = TestDb.OpenSqlite();
            using var ctx = TestDb.CreateSqlite(conn);
            var assembly = ctx.GetService<IMigrationsAssembly>();
            var type = assembly.Migrations.Values.First();
            var migration = assembly.CreateMigration(type, "Microsoft.EntityFrameworkCore.Sqlite");
            Assert.NotNull(migration.TargetModel);
            Assert.NotEmpty(migration.UpOperations);
            Assert.NotEmpty(migration.DownOperations);
        }

        [Fact]
        public async Task MigrateUpAndDown_Sqlite_Works()
        {
            using var conn = TestDb.OpenSqlite();
            using var ctx = TestDb.CreateSqlite(conn);
            await ctx.Database.MigrateAsync();
            Assert.Empty(await ctx.Database.GetPendingMigrationsAsync());
            Assert.Single(await ctx.Database.GetAppliedMigrationsAsync());

            var migrator = ctx.GetService<IMigrator>();
            await migrator.MigrateAsync("0");
            Assert.Empty(await ctx.Database.GetAppliedMigrationsAsync());
        }
    }
}
