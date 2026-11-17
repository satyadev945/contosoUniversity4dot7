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





## Migration notes (ASP.NET Core MVC on .NET 10)

- The application is now a single SDK-style ASP.NET Core MVC project (`net10.0`) using EF Core 10.
- Configure the database in `appsettings.json` (`ConnectionStrings:SchoolContext`); the default points to the existing LocalDB database `ContosoUniversity.DAL.SchoolContext`.
- A new EF Core `InitialCreate` migration is included. For an existing EF6 database, do not run it; instead insert the migration id into `__EFMigrationsHistory` (see `dotnet ef migrations script --idempotent`).
- Seed data is applied in Development when `Database:SeedOnStartup` is `true` (see `DAL/DbInitializer.cs`).
- Client libraries are served from `wwwroot/lib`; upgrade them with LibMan (jQuery 3.x, jquery-validation, Bootstrap 3.4.1/5.x) as a follow-up.
