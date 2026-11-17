using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// Seeds the database with sample data. Every step checks for existing data first,
    /// so it is safe to run on every start-up and against a pre-existing database.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(SchoolContext context)
        {
            if (await context.Students.AnyAsync())
            {
                return;
            }

            var students = new[]
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

            var instructors = new[]
            {
                new Instructor { FirstMidName = "Kim",     LastName = "Abercrombie", HireDate = DateTime.Parse("1995-03-11") },
                new Instructor { FirstMidName = "Fadi",    LastName = "Fakhouri",    HireDate = DateTime.Parse("2002-07-06") },
                new Instructor { FirstMidName = "Roger",   LastName = "Harui",       HireDate = DateTime.Parse("1998-07-01") },
                new Instructor { FirstMidName = "Candace", LastName = "Kapoor",      HireDate = DateTime.Parse("2001-01-15") },
                new Instructor { FirstMidName = "Roger",   LastName = "Zheng",       HireDate = DateTime.Parse("2004-02-12") }
            };
            context.Instructors.AddRange(instructors);
            await context.SaveChangesAsync();

            Instructor Instr(string lastName) => instructors.Single(i => i.LastName == lastName);

            var departments = new[]
            {
                new Department { Name = "English",     Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = Instr("Abercrombie").ID },
                new Department { Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = Instr("Fakhouri").ID },
                new Department { Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = Instr("Harui").ID },
                new Department { Name = "Economics",   Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = Instr("Kapoor").ID }
            };
            context.Departments.AddRange(departments);
            await context.SaveChangesAsync();

            int Dept(string name) => departments.Single(d => d.Name == name).DepartmentID;

            var courses = new[]
            {
                new Course { CourseID = 1050, Title = "Chemistry",      Credits = 3, DepartmentID = Dept("Engineering") },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = Dept("Economics") },
                new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, DepartmentID = Dept("Economics") },
                new Course { CourseID = 1045, Title = "Calculus",       Credits = 4, DepartmentID = Dept("Mathematics") },
                new Course { CourseID = 3141, Title = "Trigonometry",   Credits = 4, DepartmentID = Dept("Mathematics") },
                new Course { CourseID = 2021, Title = "Composition",    Credits = 3, DepartmentID = Dept("English") },
                new Course { CourseID = 2042, Title = "Literature",     Credits = 4, DepartmentID = Dept("English") }
            };

            Course Crs(string title) => courses.Single(c => c.Title == title);
            void Teach(string title, string lastName) => Crs(title).Instructors.Add(Instr(lastName));

            Teach("Chemistry", "Kapoor");
            Teach("Chemistry", "Harui");
            Teach("Microeconomics", "Zheng");
            Teach("Macroeconomics", "Zheng");
            Teach("Calculus", "Fakhouri");
            Teach("Trigonometry", "Harui");
            Teach("Composition", "Abercrombie");
            Teach("Literature", "Abercrombie");

            context.Courses.AddRange(courses);

            context.OfficeAssignments.AddRange(
                new OfficeAssignment { InstructorID = Instr("Fakhouri").ID, Location = "Smith 17" },
                new OfficeAssignment { InstructorID = Instr("Harui").ID,    Location = "Gowan 27" },
                new OfficeAssignment { InstructorID = Instr("Kapoor").ID,   Location = "Thompson 304" });

            await context.SaveChangesAsync();

            Student Stu(string lastName) => students.Single(s => s.LastName == lastName);

            context.Enrollments.AddRange(
                new Enrollment { StudentID = Stu("Alexander").ID, CourseID = Crs("Chemistry").CourseID,      Grade = Grade.A },
                new Enrollment { StudentID = Stu("Alexander").ID, CourseID = Crs("Microeconomics").CourseID, Grade = Grade.C },
                new Enrollment { StudentID = Stu("Alexander").ID, CourseID = Crs("Macroeconomics").CourseID, Grade = Grade.B },
                new Enrollment { StudentID = Stu("Alonso").ID,    CourseID = Crs("Calculus").CourseID,       Grade = Grade.B },
                new Enrollment { StudentID = Stu("Alonso").ID,    CourseID = Crs("Trigonometry").CourseID,   Grade = Grade.B },
                new Enrollment { StudentID = Stu("Alonso").ID,    CourseID = Crs("Composition").CourseID,    Grade = Grade.B },
                new Enrollment { StudentID = Stu("Anand").ID,     CourseID = Crs("Chemistry").CourseID },
                new Enrollment { StudentID = Stu("Anand").ID,     CourseID = Crs("Microeconomics").CourseID, Grade = Grade.B },
                new Enrollment { StudentID = Stu("Barzdukas").ID, CourseID = Crs("Chemistry").CourseID,      Grade = Grade.B },
                new Enrollment { StudentID = Stu("Li").ID,        CourseID = Crs("Composition").CourseID,    Grade = Grade.B },
                new Enrollment { StudentID = Stu("Justice").ID,   CourseID = Crs("Literature").CourseID,     Grade = Grade.B });

            await context.SaveChangesAsync();
        }
    }
}
