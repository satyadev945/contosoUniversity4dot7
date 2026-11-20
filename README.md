# University Admin Website

## ASP.NET Core MVC (.NET 10) with Entity Framework Core

Full CRUD application built with ASP.NET Core MVC on .NET 10 and Entity Framework Core 10 (SQL Server).
Add, edit, update and delete:

> Students
>
> Courses
>
> Instructors

## Running the application

1. Install the .NET 10 SDK and SQL Server (or LocalDB on Windows).
2. Set `ConnectionStrings:SchoolContext` in `ContosoUniversity/appsettings.json` (or use user-secrets / environment variables outside Development).
3. Run:

   ```
   dotnet run --project ContosoUniversity
   ```

In the Development environment the app applies EF Core migrations and seeds sample data at startup
(`Database:MigrateOnStartup`, `Database:Seed` in `appsettings.Development.json`).

### Existing databases created by the legacy EF6 application

`Database:BaselineLegacyDatabase=true` marks the EF Core `InitialCreate` migration as applied when the legacy
`Student` table already exists, so the existing schema and data are reused.

### Migrations

```
dotnet ef migrations add <Name> --project ContosoUniversity --output-dir Data/Migrations
dotnet ef database update --project ContosoUniversity
```

## Client libraries

Managed with LibMan (`ContosoUniversity/libman.json`) into `wwwroot/lib`: jQuery 3.7.1, Bootstrap 3.4.1,
jQuery Validation 1.19.5 and jQuery Validation Unobtrusive 4.0.0.

## Known legacy defect (kept for parity)

The "Departments" menu link points to a `Department` controller that does not exist (404), as in the original application.

## Project preview

![](./images/university1.png)

![](./images/university2.png)
