using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// Applies pending migrations and seeds sample data when the database is empty.
    /// Replaces the EF6 DropCreateDatabaseIfModelChanges initializer and migrations seed.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(SchoolContext context)
        {
            await context.Database.MigrateAsync();

            if (await context.Students.AnyAsync())
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
            await context.SaveChangesAsync();

            Instructor Instructor(string lastName) => instructors.Single(i => i.LastName == lastName);

            var departments = new[]
            {
                new Department { Name = "English",     Budget = 350000, StartDate = new DateTime(2007, 9, 1), InstructorID = Instructor("Abercrombie").ID },
                new Department { Name = "Mathematics", Budget = 100000, StartDate = new DateTime(2007, 9, 1), InstructorID = Instructor("Fakhouri").ID },
                new Department { Name = "Engineering", Budget = 350000, StartDate = new DateTime(2007, 9, 1), InstructorID = Instructor("Harui").ID },
                new Department { Name = "Economics",   Budget = 100000, StartDate = new DateTime(2007, 9, 1), InstructorID = Instructor("Kapoor").ID }
            };
            context.Departments.AddRange(departments);
            await context.SaveChangesAsync();

            int Department(string name) => departments.Single(d => d.Name == name).DepartmentID;

            var courses = new[]
            {
                new Course { CourseID = 1050, Title = "Chemistry",      Credits = 3, DepartmentID = Department("Engineering"),
                    Instructors = new List<Instructor> { Instructor("Kapoor"), Instructor("Harui") } },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = Department("Economics"),
                    Instructors = new List<Instructor> { Instructor("Zheng") } },
                new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, DepartmentID = Department("Economics"),
                    Instructors = new List<Instructor> { Instructor("Zheng") } },
                new Course { CourseID = 1045, Title = "Calculus",       Credits = 4, DepartmentID = Department("Mathematics"),
                    Instructors = new List<Instructor> { Instructor("Fakhouri") } },
                new Course { CourseID = 3141, Title = "Trigonometry",   Credits = 4, DepartmentID = Department("Mathematics"),
                    Instructors = new List<Instructor> { Instructor("Harui") } },
                new Course { CourseID = 2021, Title = "Composition",    Credits = 3, DepartmentID = Department("English"),
                    Instructors = new List<Instructor> { Instructor("Abercrombie") } },
                new Course { CourseID = 2042, Title = "Literature",     Credits = 4, DepartmentID = Department("English"),
                    Instructors = new List<Instructor> { Instructor("Abercrombie") } }
            };
            context.Courses.AddRange(courses);

            context.OfficeAssignments.AddRange(
                new OfficeAssignment { InstructorID = Instructor("Fakhouri").ID, Location = "Smith 17" },
                new OfficeAssignment { InstructorID = Instructor("Harui").ID,    Location = "Gowan 27" },
                new OfficeAssignment { InstructorID = Instructor("Kapoor").ID,   Location = "Thompson 304" });
            await context.SaveChangesAsync();

            Student Student(string lastName) => students.Single(s => s.LastName == lastName);
            Course Course(string title) => courses.Single(c => c.Title == title);

            context.Enrollments.AddRange(
                new Enrollment { StudentID = Student("Alexander").ID, CourseID = Course("Chemistry").CourseID,      Grade = Grade.A },
                new Enrollment { StudentID = Student("Alexander").ID, CourseID = Course("Microeconomics").CourseID, Grade = Grade.C },
                new Enrollment { StudentID = Student("Alexander").ID, CourseID = Course("Macroeconomics").CourseID, Grade = Grade.B },
                new Enrollment { StudentID = Student("Alonso").ID,    CourseID = Course("Calculus").CourseID,       Grade = Grade.B },
                new Enrollment { StudentID = Student("Alonso").ID,    CourseID = Course("Trigonometry").CourseID,   Grade = Grade.B },
                new Enrollment { StudentID = Student("Alonso").ID,    CourseID = Course("Composition").CourseID,    Grade = Grade.B },
                new Enrollment { StudentID = Student("Anand").ID,     CourseID = Course("Chemistry").CourseID },
                new Enrollment { StudentID = Student("Anand").ID,     CourseID = Course("Microeconomics").CourseID, Grade = Grade.B },
                new Enrollment { StudentID = Student("Barzdukas").ID, CourseID = Course("Chemistry").CourseID,      Grade = Grade.B },
                new Enrollment { StudentID = Student("Li").ID,        CourseID = Course("Composition").CourseID,    Grade = Grade.B },
                new Enrollment { StudentID = Student("Justice").ID,   CourseID = Course("Literature").CourseID,     Grade = Grade.B });
            await context.SaveChangesAsync();
        }
    }
}
