# University Admin Website 

## Asp.net MVC 5 with Entity Framework

Full CRUD operation ASP.NET MVC 5 framework intergrated with Entity Framework 6 SQL database. 
Add edit update and delete 
> Students 
>
> Courses
> 
> Instructors
> 
> Room bookings.




## Project preview

![](./images/university1.png)

![](./images/university2.png)





## Migration notes (ASP.NET MVC 5 / .NET Framework 4.7 -> ASP.NET Core MVC on .NET 10)

- Single SDK-style project `ContosoUniversity/` (net10.0) plus `ContosoUniversity.Tests/` (xUnit). Build with `dotnet build ContosoUniversity.slnx`.
- Entry point and middleware pipeline live in `Program.cs` (replaces Global.asax, RouteConfig, FilterConfig, BundleConfig).
- Configuration is in `appsettings.json` (`ConnectionStrings:SchoolContext`). Verify the database name before first run.
- EF Core 10 (`Data/SchoolContext.cs`). A fresh `InitialCreate` migration replaces the six EF6 migrations. For an existing EF6-created database,
  do not apply it blindly: generate an idempotent script (`dotnet ef migrations script --idempotent`) and insert the `InitialCreate`
  migration id into `__EFMigrationsHistory` instead. In Development the app runs `Database.MigrateAsync()` and seeds sample data (`Data/DbInitializer.cs`).
- Static assets are served from `wwwroot` (jQuery 3.7.1, Bootstrap 3.4.1, jQuery Validation 1.21.0) and bundled with LigerShark.WebOptimizer.
- Application Insights is enabled when `ApplicationInsights:ConnectionString` or `APPLICATIONINSIGHTS_CONNECTION_STRING` is set.
- The "Departments" nav link was removed because no Department controller/views ever existed.
- No authentication existed in the legacy app; antiforgery validation is applied globally.
