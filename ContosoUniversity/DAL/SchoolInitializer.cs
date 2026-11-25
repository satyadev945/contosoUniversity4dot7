using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.DAL
{
    public static class SchoolInitializer
    {
        public static void Initialize(SchoolContext context)
        {
            // The database may not be reachable immediately when running in containers; retry a few times.
            const int maxAttempts = 10;
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    context.Database.EnsureCreated();
                    break;
                }
                catch (Exception) when (attempt < maxAttempts)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                }
            }

            if (context.Students.Any())
            {
                return;
            }

            var department = new Department { Name = "General", Budget = 100000, StartDate = DateTime.Parse("2000-09-01") };
            context.Departments.Add(department);
            context.SaveChanges();

            var students = new List<Student>
            {
                new Student{FirstMidName="Carson",LastName="Alexander",EnrollmentDate=DateTime.Parse("2005-09-01")},
                new Student{FirstMidName="Meredith",LastName="Alonso",EnrollmentDate=DateTime.Parse("2002-09-01")},
                new Student{FirstMidName="Arturo",LastName="Anand",EnrollmentDate=DateTime.Parse("2003-09-01")},
                new Student{FirstMidName="Gytis",LastName="Barzdukas",EnrollmentDate=DateTime.Parse("2002-09-01")},
                new Student{FirstMidName="Yan",LastName="Li",EnrollmentDate=DateTime.Parse("2002-09-01")},
                new Student{FirstMidName="Peggy",LastName="Justice",EnrollmentDate=DateTime.Parse("2001-09-01")},
                new Student{FirstMidName="Laura",LastName="Norman",EnrollmentDate=DateTime.Parse("2003-09-01")},
                new Student{FirstMidName="Nino",LastName="Olivetto",EnrollmentDate=DateTime.Parse("2005-09-01")}
            };
            context.Students.AddRange(students);
            context.SaveChanges();

            var courses = new List<Course>
            {
                new Course{CourseID=1050,Title="Chemistry",Credits=3,DepartmentID=department.DepartmentID},
                new Course{CourseID=4022,Title="Microeconomics",Credits=3,DepartmentID=department.DepartmentID},
                new Course{CourseID=4041,Title="Macroeconomics",Credits=3,DepartmentID=department.DepartmentID},
                new Course{CourseID=1045,Title="Calculus",Credits=4,DepartmentID=department.DepartmentID},
                new Course{CourseID=3141,Title="Trigonometry",Credits=4,DepartmentID=department.DepartmentID},
                new Course{CourseID=2021,Title="Composition",Credits=3,DepartmentID=department.DepartmentID},
                new Course{CourseID=2042,Title="Literature",Credits=4,DepartmentID=department.DepartmentID}
            };
            context.Courses.AddRange(courses);
            context.SaveChanges();

            var enrollments = new List<Enrollment>
            {
                new Enrollment{StudentID=students[0].ID,CourseID=1050,Grade=Grade.A},
                new Enrollment{StudentID=students[0].ID,CourseID=4022,Grade=Grade.C},
                new Enrollment{StudentID=students[0].ID,CourseID=4041,Grade=Grade.B},
                new Enrollment{StudentID=students[1].ID,CourseID=1045,Grade=Grade.B},
                new Enrollment{StudentID=students[1].ID,CourseID=3141,Grade=Grade.F},
                new Enrollment{StudentID=students[1].ID,CourseID=2021,Grade=Grade.F},
                new Enrollment{StudentID=students[2].ID,CourseID=1050},
                new Enrollment{StudentID=students[3].ID,CourseID=1050},
                new Enrollment{StudentID=students[3].ID,CourseID=4022,Grade=Grade.F},
                new Enrollment{StudentID=students[4].ID,CourseID=4041,Grade=Grade.C},
                new Enrollment{StudentID=students[5].ID,CourseID=1045},
                new Enrollment{StudentID=students[6].ID,CourseID=3141,Grade=Grade.A}
            };
            context.Enrollments.AddRange(enrollments);
            context.SaveChanges();
        }
    }
}
