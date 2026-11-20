using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// Applies EF Core migrations and optionally seeds data at startup.
    /// Can baseline a database that was created by the legacy EF6 application so that the
    /// EF Core InitialCreate migration is not re-applied over existing tables.
    /// </summary>
    public static class DatabaseInitializer
    {
        private const string BaselineMigrationSuffix = "_InitialCreate";

        public static async Task InitializeAsync(IServiceProvider services, bool baselineLegacyDatabase, bool seed)
        {
            using var scope = services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
            var context = scope.ServiceProvider.GetRequiredService<SchoolContext>();

            try
            {
                if (baselineLegacyDatabase)
                {
                    await BaselineLegacyDatabaseAsync(context, logger);
                }

                await context.Database.MigrateAsync();

                if (seed)
                {
                    await DbSeeder.SeedAsync(context);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating or seeding the database.");
                throw;
            }
        }

        private static async Task BaselineLegacyDatabaseAsync(SchoolContext context, ILogger logger)
        {
            if (!await context.Database.CanConnectAsync())
            {
                return;
            }

            var applied = (await context.Database.GetAppliedMigrationsAsync()).Any();
            if (applied)
            {
                return;
            }

            var legacyTableExists = await TableExistsAsync(context, "Student");
            if (!legacyTableExists)
            {
                return;
            }

            var firstMigration = context.Database.GetMigrations().FirstOrDefault(m => m.EndsWith(BaselineMigrationSuffix, StringComparison.Ordinal));
            if (firstMigration == null)
            {
                return;
            }

            logger.LogWarning("Legacy EF6 schema detected. Marking migration {Migration} as applied (baseline).", firstMigration);

            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END");

            await context.Database.ExecuteSqlRawAsync(
                "IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = {0}) INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ({0}, {1})",
                firstMigration,
                "10.0.0");
        }

        private static async Task<bool> TableExistsAsync(SchoolContext context, string tableName)
        {
            var connection = context.Database.GetDbConnection();
            var wasClosed = connection.State != System.Data.ConnectionState.Open;
            if (wasClosed)
            {
                await connection.OpenAsync();
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT CASE WHEN OBJECT_ID(@name, N'U') IS NULL THEN 0 ELSE 1 END";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@name";
                parameter.Value = tableName;
                command.Parameters.Add(parameter);
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) == 1;
            }
            finally
            {
                if (wasClosed)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}
