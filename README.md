# University Admin Website 

## ASP.NET Core MVC (.NET 10) with Entity Framework Core

Full CRUD operation ASP.NET Core MVC (.NET 10) integrated with Entity Framework Core 10 and SQL Server. 
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






## Running

```
dotnet build ContosoUniversity.sln
dotnet run --project ContosoUniversity
```

The connection string lives in `ContosoUniversity/appsettings.json` (`ConnectionStrings:SchoolContext`).
With `Database:InitializeOnStartup=true` the app applies EF Core migrations and seeds an empty database at startup.

### Existing EF6 database

An existing EF6 database must be baselined before the EF Core migration can run against it: create the
`__EFMigrationsHistory` table and insert the `InitialCreate` migration id (with ProductVersion `10.0.0`),
or set `Database:InitializeOnStartup` to `false`.
