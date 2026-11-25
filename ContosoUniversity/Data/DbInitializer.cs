using System;
using System.Collections.Generic;
using System.Linq;
using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// Applies pending migrations and seeds the database when it is empty.
    /// Replaces the EF6 SchoolInitializer / Migrations.Configuration.Seed (AddOrUpdate) logic.
    /// The database is never dropped automatically.
    /// </summary>
    public static class DbInitializer
    {
        public static void Initialize(SchoolContext context)
        {
            context.Database.Migrate();

            if (context.Students.Any())
            {
                return;
            }

            var students = new List<Student>
            {
                new Student { FirstMidName = "Carson",   LastName = "Alexander", EnrollmentDate = new DateTime(2010, 9, 1) },
                new Student { FirstMidName = "Meredith", LastName = "Alonso",    EnrollmentDate = new DateTime(2012, 9, 1) },
                new Student { FirstMidName = "Arturo",   LastName = "Anand",     EnrollmentDate = new DateTime(2013, 9, 1) },
                new Student { FirstMidName = "Gytis",    LastName = "Barzdukas", EnrollmentDate = new DateTime(2012, 9, 1) },
                new Student { FirstMidName = "Yan",      LastName = "Li",        EnrollmentDate = new DateTime(2012, 9, 1) },
                new Student { FirstMidName = "Peggy",    LastName = "Justice",   EnrollmentDate = new DateTime(2011, 9, 1) },
                new Student { FirstMidName = "Laura",    LastName = "Norman",    EnrollmentDate = new DateTime(2013, 9, 1) },
                new Student { FirstMidName = "Nino",     LastName = "Olivetto",  EnrollmentDate = new DateTime(2005, 9, 1) }
            };
            context.Students.AddRange(students);

            var instructors = new List<Instructor>
            {
                new Instructor { FirstMidName = "Kim",     LastName = "Abercrombie", HireDate = new DateTime(1995, 3, 11) },
                new Instructor { FirstMidName = "Fadi",    LastName = "Fakhouri",    HireDate = new DateTime(2002, 7, 6) },
                new Instructor { FirstMidName = "Roger",   LastName = "Harui",       HireDate = new DateTime(1998, 7, 1) },
                new Instructor { FirstMidName = "Candace", LastName = "Kapoor",      HireDate = new DateTime(2001, 1, 15) },
                new Instructor { FirstMidName = "Roger",   LastName = "Zheng",       HireDate = new DateTime(2004, 2, 12) }
            };
            context.Instructors.AddRange(instructors);
            context.SaveChanges();

            Instructor Teacher(string lastName) => instructors.Single(i => i.LastName == lastName);

            var departments = new List<Department>
            {
                new Department { Name = "English",     Budget = 350000, StartDate = new DateTime(2007, 9, 1), Administrator = Teacher("Abercrombie") },
                new Department { Name = "Mathematics", Budget = 100000, StartDate = new DateTime(2007, 9, 1), Administrator = Teacher("Fakhouri") },
                new Department { Name = "Engineering", Budget = 350000, StartDate = new DateTime(2007, 9, 1), Administrator = Teacher("Harui") },
                new Department { Name = "Economics",   Budget = 100000, StartDate = new DateTime(2007, 9, 1), Administrator = Teacher("Kapoor") }
            };
            context.Departments.AddRange(departments);

            Department Dept(string name) => departments.Single(d => d.Name == name);

            var courses = new List<Course>
            {
                new Course { CourseID = 1050, Title = "Chemistry",      Credits = 3, Department = Dept("Engineering"),
                    Instructors = new List<Instructor> { Teacher("Kapoor"), Teacher("Harui") } },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, Department = Dept("Economics"),
                    Instructors = new List<Instructor> { Teacher("Zheng") } },
                new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, Department = Dept("Economics"),
                    Instructors = new List<Instructor> { Teacher("Zheng") } },
                new Course { CourseID = 1045, Title = "Calculus",       Credits = 4, Department = Dept("Mathematics"),
                    Instructors = new List<Instructor> { Teacher("Fakhouri") } },
                new Course { CourseID = 3141, Title = "Trigonometry",   Credits = 4, Department = Dept("Mathematics"),
                    Instructors = new List<Instructor> { Teacher("Harui") } },
                new Course { CourseID = 2021, Title = "Composition",    Credits = 3, Department = Dept("English"),
                    Instructors = new List<Instructor> { Teacher("Abercrombie") } },
                new Course { CourseID = 2042, Title = "Literature",     Credits = 4, Department = Dept("English"),
                    Instructors = new List<Instructor> { Teacher("Abercrombie") } }
            };
            context.Courses.AddRange(courses);

            context.OfficeAssignments.AddRange(
                new OfficeAssignment { Instructor = Teacher("Fakhouri"), Location = "Smith 17" },
                new OfficeAssignment { Instructor = Teacher("Harui"),    Location = "Gowan 27" },
                new OfficeAssignment { Instructor = Teacher("Kapoor"),   Location = "Thompson 304" });

            Student Pupil(string lastName) => students.Single(s => s.LastName == lastName);
            Course Subject(string title) => courses.Single(c => c.Title == title);

            context.Enrollments.AddRange(
                new Enrollment { Student = Pupil("Alexander"), Course = Subject("Chemistry"),      Grade = Grade.A },
                new Enrollment { Student = Pupil("Alexander"), Course = Subject("Microeconomics"), Grade = Grade.C },
                new Enrollment { Student = Pupil("Alexander"), Course = Subject("Macroeconomics"), Grade = Grade.B },
                new Enrollment { Student = Pupil("Alonso"),    Course = Subject("Calculus"),       Grade = Grade.B },
                new Enrollment { Student = Pupil("Alonso"),    Course = Subject("Trigonometry"),   Grade = Grade.B },
                new Enrollment { Student = Pupil("Alonso"),    Course = Subject("Composition"),    Grade = Grade.B },
                new Enrollment { Student = Pupil("Anand"),     Course = Subject("Chemistry") },
                new Enrollment { Student = Pupil("Anand"),     Course = Subject("Microeconomics"), Grade = Grade.B },
                new Enrollment { Student = Pupil("Barzdukas"), Course = Subject("Chemistry"),      Grade = Grade.B },
                new Enrollment { Student = Pupil("Li"),        Course = Subject("Composition"),    Grade = Grade.B },
                new Enrollment { Student = Pupil("Justice"),   Course = Subject("Literature"),     Grade = Grade.B });

            context.SaveChanges();
        }
    }
}
