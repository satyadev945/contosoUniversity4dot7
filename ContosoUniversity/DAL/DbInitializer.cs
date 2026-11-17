using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.DAL
{
    public static class DbInitializer
    {
        /// <summary>
        /// Seeds sample data when the database does not contain any students yet.
        /// </summary>
        public static async Task InitializeAsync(SchoolContext context, CancellationToken cancellationToken = default)
        {
            if (await context.Students.AnyAsync(cancellationToken))
            {
                return;
            }

            var students = new[]
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

            var instructors = new[]
            {
                new Instructor { FirstMidName = "Kim",     LastName = "Abercrombie", HireDate = new DateTime(1995, 3, 11) },
                new Instructor { FirstMidName = "Fadi",    LastName = "Fakhouri",    HireDate = new DateTime(2002, 7, 6) },
                new Instructor { FirstMidName = "Roger",   LastName = "Harui",       HireDate = new DateTime(1998, 7, 1) },
                new Instructor { FirstMidName = "Candace", LastName = "Kapoor",      HireDate = new DateTime(2001, 1, 15) },
                new Instructor { FirstMidName = "Roger",   LastName = "Zheng",       HireDate = new DateTime(2004, 2, 12) }
            };
            context.Instructors.AddRange(instructors);
            await context.SaveChangesAsync(cancellationToken);

            Instructor Instr(string lastName) => instructors.Single(i => i.LastName == lastName);

            var departments = new[]
            {
                new Department { Name = "English",     Budget = 350000, StartDate = new DateTime(2007, 9, 1), InstructorID = Instr("Abercrombie").ID },
                new Department { Name = "Mathematics", Budget = 100000, StartDate = new DateTime(2007, 9, 1), InstructorID = Instr("Fakhouri").ID },
                new Department { Name = "Engineering", Budget = 350000, StartDate = new DateTime(2007, 9, 1), InstructorID = Instr("Harui").ID },
                new Department { Name = "Economics",   Budget = 100000, StartDate = new DateTime(2007, 9, 1), InstructorID = Instr("Kapoor").ID }
            };
            context.Departments.AddRange(departments);
            await context.SaveChangesAsync(cancellationToken);

            int Dept(string name) => departments.Single(d => d.Name == name).DepartmentID;

            var courses = new[]
            {
                new Course { CourseID = 1050, Title = "Chemistry",      Credits = 3, DepartmentID = Dept("Engineering"), Instructors = new List<Instructor> { Instr("Kapoor"), Instr("Harui") } },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = Dept("Economics"),   Instructors = new List<Instructor> { Instr("Zheng") } },
                new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, DepartmentID = Dept("Economics"),   Instructors = new List<Instructor> { Instr("Zheng") } },
                new Course { CourseID = 1045, Title = "Calculus",       Credits = 4, DepartmentID = Dept("Mathematics"), Instructors = new List<Instructor> { Instr("Fakhouri") } },
                new Course { CourseID = 3141, Title = "Trigonometry",   Credits = 4, DepartmentID = Dept("Mathematics"), Instructors = new List<Instructor> { Instr("Harui") } },
                new Course { CourseID = 2021, Title = "Composition",    Credits = 3, DepartmentID = Dept("English"),     Instructors = new List<Instructor> { Instr("Abercrombie") } },
                new Course { CourseID = 2042, Title = "Literature",     Credits = 4, DepartmentID = Dept("English"),     Instructors = new List<Instructor> { Instr("Abercrombie") } }
            };
            context.Courses.AddRange(courses);

            context.OfficeAssignments.AddRange(
                new OfficeAssignment { InstructorID = Instr("Fakhouri").ID, Location = "Smith 17" },
                new OfficeAssignment { InstructorID = Instr("Harui").ID,    Location = "Gowan 27" },
                new OfficeAssignment { InstructorID = Instr("Kapoor").ID,   Location = "Thompson 304" });
            await context.SaveChangesAsync(cancellationToken);

            int Stud(string lastName) => students.Single(s => s.LastName == lastName).ID;

            context.Enrollments.AddRange(
                new Enrollment { StudentID = Stud("Alexander"), CourseID = 1050, Grade = Grade.A },
                new Enrollment { StudentID = Stud("Alexander"), CourseID = 4022, Grade = Grade.C },
                new Enrollment { StudentID = Stud("Alexander"), CourseID = 4041, Grade = Grade.B },
                new Enrollment { StudentID = Stud("Alonso"),    CourseID = 1045, Grade = Grade.B },
                new Enrollment { StudentID = Stud("Alonso"),    CourseID = 3141, Grade = Grade.B },
                new Enrollment { StudentID = Stud("Alonso"),    CourseID = 2021, Grade = Grade.B },
                new Enrollment { StudentID = Stud("Anand"),     CourseID = 1050 },
                new Enrollment { StudentID = Stud("Anand"),     CourseID = 4022, Grade = Grade.B },
                new Enrollment { StudentID = Stud("Barzdukas"), CourseID = 1050, Grade = Grade.B },
                new Enrollment { StudentID = Stud("Li"),        CourseID = 2021, Grade = Grade.B },
                new Enrollment { StudentID = Stud("Justice"),   CourseID = 2042, Grade = Grade.B });
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
