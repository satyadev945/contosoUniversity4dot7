# University Admin Website 

## ASP.NET Core MVC (.NET 8) with Entity Framework Core

Full CRUD operation ASP.NET Core MVC integrated with Entity Framework Core (SQL Server). 
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





## Running in a Linux container

The app targets ASP.NET Core (net8.0) and runs on Linux containers.

```
docker build -t contosouniversity ./ContosoUniversity
docker run -p 8080:8080 -e ConnectionStrings__SchoolContext="Server=<host>;Database=ContosoUniversity;User Id=<user>;Password=<pwd>;TrustServerCertificate=True" contosouniversity
```

Health check endpoint: `GET /health`.
