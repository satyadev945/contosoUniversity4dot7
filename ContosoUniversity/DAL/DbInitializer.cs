using System;
using System.Collections.Generic;
using System.Linq;
using ContosoUniversity.Models;

namespace ContosoUniversity.DAL
{
    public static class DbInitializer
    {
        public static void Initialize(SchoolContext context)
        {
            context.Database.EnsureCreated();

            if (context.Students.Any())
            {
                return; // already seeded
            }

            var students = new List<Student>
            {
                new Student { FirstMidName = "Carson",   LastName = "Alexander", EnrollmentDate = DateTime.Parse("2010-09-01") },
                new Student { FirstMidName = "Meredith", LastName = "Alonso",    EnrollmentDate = DateTime.Parse("2012-09-01") },
                new Student { FirstMidName = "Arturo",   LastName = "Anand",     EnrollmentDate = DateTime.Parse("2013-09-01") },
                new Student { FirstMidName = "Gytis",    LastName = "Barzdukas", EnrollmentDate = DateTime.Parse("2012-09-01") },
                new Student { FirstMidName = "Yan",      LastName = "Li",        EnrollmentDate = DateTime.Parse("2012-09-01") },
                new Student { FirstMidName = "Peggy",    LastName = "Justice",   EnrollmentDate = DateTime.Parse("2011-09-01") },
                new Student { FirstMidName = "Laura",    LastName = "Norman",    EnrollmentDate = DateTime.Parse("2013-09-01") },
                new Student { FirstMidName = "Nino",     LastName = "Olivetto",  EnrollmentDate = DateTime.Parse("2005-09-01") }
            };
            context.Students.AddRange(students);
            context.SaveChanges();

            var instructors = new List<Instructor>
            {
                new Instructor { FirstMidName = "Kim",     LastName = "Abercrombie", HireDate = DateTime.Parse("1995-03-11") },
                new Instructor { FirstMidName = "Fadi",    LastName = "Fakhouri",    HireDate = DateTime.Parse("2002-07-06") },
                new Instructor { FirstMidName = "Roger",   LastName = "Harui",       HireDate = DateTime.Parse("1998-07-01") },
                new Instructor { FirstMidName = "Candace", LastName = "Kapoor",      HireDate = DateTime.Parse("2001-01-15") },
                new Instructor { FirstMidName = "Roger",   LastName = "Zheng",       HireDate = DateTime.Parse("2004-02-12") }
            };
            context.Instructors.AddRange(instructors);
            context.SaveChanges();

            var departments = new List<Department>
            {
                new Department { Name = "English",     Budget = 350000, StartDate = DateTime.Parse("2007-09-01"),
                    InstructorID = instructors.Single(i => i.LastName == "Abercrombie").ID },
                new Department { Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"),
                    InstructorID = instructors.Single(i => i.LastName == "Fakhouri").ID },
                new Department { Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"),
                    InstructorID = instructors.Single(i => i.LastName == "Harui").ID },
                new Department { Name = "Economics",   Budget = 100000, StartDate = DateTime.Parse("2007-09-01"),
                    InstructorID = instructors.Single(i => i.LastName == "Kapoor").ID }
            };
            context.Departments.AddRange(departments);
            context.SaveChanges();

            var courses = new List<Course>
            {
                new Course { CourseID = 1050, Title = "Chemistry",      Credits = 3,
                    DepartmentID = departments.Single(s => s.Name == "Engineering").DepartmentID,
                    Instructors = new List<Instructor>() },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3,
                    DepartmentID = departments.Single(s => s.Name == "Economics").DepartmentID,
                    Instructors = new List<Instructor>() },
                new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3,
                    DepartmentID = departments.Single(s => s.Name == "Economics").DepartmentID,
                    Instructors = new List<Instructor>() },
                new Course { CourseID = 1045, Title = "Calculus",       Credits = 4,
                    DepartmentID = departments.Single(s => s.Name == "Mathematics").DepartmentID,
                    Instructors = new List<Instructor>() },
                new Course { CourseID = 3141, Title = "Trigonometry",   Credits = 4,
                    DepartmentID = departments.Single(s => s.Name == "Mathematics").DepartmentID,
                    Instructors = new List<Instructor>() },
                new Course { CourseID = 2021, Title = "Composition",    Credits = 3,
                    DepartmentID = departments.Single(s => s.Name == "English").DepartmentID,
                    Instructors = new List<Instructor>() },
                new Course { CourseID = 2042, Title = "Literature",     Credits = 4,
                    DepartmentID = departments.Single(s => s.Name == "English").DepartmentID,
                    Instructors = new List<Instructor>() }
            };

            AddInstructor(courses, instructors, "Chemistry", "Kapoor");
            AddInstructor(courses, instructors, "Chemistry", "Harui");
            AddInstructor(courses, instructors, "Microeconomics", "Zheng");
            AddInstructor(courses, instructors, "Macroeconomics", "Zheng");
            AddInstructor(courses, instructors, "Calculus", "Fakhouri");
            AddInstructor(courses, instructors, "Trigonometry", "Harui");
            AddInstructor(courses, instructors, "Composition", "Abercrombie");
            AddInstructor(courses, instructors, "Literature", "Abercrombie");

            context.Courses.AddRange(courses);
            context.SaveChanges();

            var officeAssignments = new List<OfficeAssignment>
            {
                new OfficeAssignment { InstructorID = instructors.Single(i => i.LastName == "Fakhouri").ID, Location = "Smith 17" },
                new OfficeAssignment { InstructorID = instructors.Single(i => i.LastName == "Harui").ID,    Location = "Gowan 27" },
                new OfficeAssignment { InstructorID = instructors.Single(i => i.LastName == "Kapoor").ID,   Location = "Thompson 304" }
            };
            context.OfficeAssignments.AddRange(officeAssignments);
            context.SaveChanges();

            var enrollments = new List<Enrollment>
            {
                new Enrollment { StudentID = students.Single(s => s.LastName == "Alexander").ID, CourseID = 1050, Grade = Grade.A },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Alexander").ID, CourseID = 4022, Grade = Grade.C },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Alexander").ID, CourseID = 4041, Grade = Grade.B },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Alonso").ID,    CourseID = 1045, Grade = Grade.B },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Alonso").ID,    CourseID = 3141, Grade = Grade.B },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Alonso").ID,    CourseID = 2021, Grade = Grade.B },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Anand").ID,     CourseID = 1050 },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Anand").ID,     CourseID = 4022, Grade = Grade.B },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Barzdukas").ID, CourseID = 1050, Grade = Grade.B },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Li").ID,        CourseID = 2021, Grade = Grade.B },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Justice").ID,   CourseID = 2042, Grade = Grade.B }
            };
            context.Enrollments.AddRange(enrollments);
            context.SaveChanges();
        }

        private static void AddInstructor(List<Course> courses, List<Instructor> instructors, string courseTitle, string instructorName)
        {
            var course = courses.Single(c => c.Title == courseTitle);
            var instructor = instructors.Single(i => i.LastName == instructorName);
            if (!course.Instructors.Contains(instructor))
            {
                course.Instructors.Add(instructor);
            }
        }
    }
}
