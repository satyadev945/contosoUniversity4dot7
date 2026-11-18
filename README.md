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





## Migration to ASP.NET Core MVC (.NET 10)

The application now targets ASP.NET Core MVC on .NET 10 with Entity Framework Core 10.

- Configure `ConnectionStrings:DefaultConnection` in `ContosoUniversity/appsettings.json` (use user-secrets or environment variables for real credentials).
- On startup the app applies EF Core migrations and seeds data (`Database:InitializeOnStartup`).
- Existing EF6 database: it already contains the tables created by EF6, so mark the `InitialCreate` migration as applied once
  (insert its id and `10.0.0` into `__EFMigrationsHistory`) or set `Database:InitializeOnStartup` to `false`.
- Run with `dotnet run --project ContosoUniversity`.
