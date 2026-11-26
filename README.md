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





## Migration notes (ASP.NET MVC 4.7 -> ASP.NET Core MVC on .NET 10)

- Connection string: `ConnectionStrings:SchoolContext` in `appsettings.json` (defaults to the database name EF6 used by convention).
- Existing EF6 databases: the EF Core `InitialBaseline` migration creates the full schema. For a database that already has the schema, create `__EFMigrationsHistory` and insert the `InitialBaseline` row (ProductVersion `10.0.0`) instead of running it.
- `Database:InitializeOnStartup` (true in Development) applies migrations and seeds data via `DAL/DbInitializer.cs`.
- Static assets live in `wwwroot/lib`; upgrade jQuery, Bootstrap and jquery-validation (via LibMan) as a follow-up.
