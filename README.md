# University Admin Website 

## ASP.NET Core 8 MVC (Linux container ready) with Entity Framework 6

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





## Running in a Linux container

The application targets ASP.NET Core (net8.0) and runs on Linux containers.

```
docker build -t contosouniversity -f ContosoUniversity/Dockerfile .
docker run -p 8080:8080 -e ConnectionStrings__SchoolContext="<sql server connection string>" contosouniversity
```

Configuration is provided through environment variables (`ConnectionStrings__SchoolContext` or `DB_CONNECTION_STRING`).
Health endpoint: `GET /health`.
