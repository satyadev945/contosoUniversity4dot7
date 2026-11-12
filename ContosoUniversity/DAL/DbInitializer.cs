using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.DAL
{
    /// <summary>
    /// Idempotent seeder that replaces the EF6 DropCreateDatabaseIfModelChanges initializer
    /// and the migrations Configuration.Seed method. Existing rows are never duplicated.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(SchoolContext context, CancellationToken cancellationToken = default)
        {
            if (await context.Students.AnyAsync(cancellationToken))
            {
                return; // Database already seeded.
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
            await context.SaveChangesAsync(cancellationToken);

            Instructor Instructor(string lastName) => instructors.Single(i => i.LastName == lastName);

            var departments = new List<Department>
            {
                new Department { Name = "English",     Budget = 350000, StartDate = new DateTime(2007, 9, 1), InstructorID = Instructor("Abercrombie").ID },
                new Department { Name = "Mathematics", Budget = 100000, StartDate = new DateTime(2007, 9, 1), InstructorID = Instructor("Fakhouri").ID },
                new Department { Name = "Engineering", Budget = 350000, StartDate = new DateTime(2007, 9, 1), InstructorID = Instructor("Harui").ID },
                new Department { Name = "Economics",   Budget = 100000, StartDate = new DateTime(2007, 9, 1), InstructorID = Instructor("Kapoor").ID }
            };
            context.Departments.AddRange(departments);
            await context.SaveChangesAsync(cancellationToken);

            int Department(string name) => departments.Single(d => d.Name == name).DepartmentID;

            var courses = new List<Course>
            {
                new Course { CourseID = 1050, Title = "Chemistry",      Credits = 3, DepartmentID = Department("Engineering") },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = Department("Economics") },
                new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, DepartmentID = Department("Economics") },
                new Course { CourseID = 1045, Title = "Calculus",       Credits = 4, DepartmentID = Department("Mathematics") },
                new Course { CourseID = 3141, Title = "Trigonometry",   Credits = 4, DepartmentID = Department("Mathematics") },
                new Course { CourseID = 2021, Title = "Composition",    Credits = 3, DepartmentID = Department("English") },
                new Course { CourseID = 2042, Title = "Literature",     Credits = 4, DepartmentID = Department("English") }
            };

            void Assign(string courseTitle, string instructorLastName) =>
                courses.Single(c => c.Title == courseTitle).Instructors.Add(Instructor(instructorLastName));

            Assign("Chemistry", "Kapoor");
            Assign("Chemistry", "Harui");
            Assign("Microeconomics", "Zheng");
            Assign("Macroeconomics", "Zheng");
            Assign("Calculus", "Fakhouri");
            Assign("Trigonometry", "Harui");
            Assign("Composition", "Abercrombie");
            Assign("Literature", "Abercrombie");
            context.Courses.AddRange(courses);

            context.OfficeAssignments.AddRange(
                new OfficeAssignment { InstructorID = Instructor("Fakhouri").ID, Location = "Smith 17" },
                new OfficeAssignment { InstructorID = Instructor("Harui").ID,    Location = "Gowan 27" },
                new OfficeAssignment { InstructorID = Instructor("Kapoor").ID,   Location = "Thompson 304" });
            await context.SaveChangesAsync(cancellationToken);

            Enrollment Enroll(string lastName, string courseTitle, Grade? grade = null) => new Enrollment
            {
                StudentID = students.Single(s => s.LastName == lastName).ID,
                CourseID = courses.Single(c => c.Title == courseTitle).CourseID,
                Grade = grade
            };

            context.Enrollments.AddRange(
                Enroll("Alexander", "Chemistry", Grade.A),
                Enroll("Alexander", "Microeconomics", Grade.C),
                Enroll("Alexander", "Macroeconomics", Grade.B),
                Enroll("Alonso", "Calculus", Grade.B),
                Enroll("Alonso", "Trigonometry", Grade.B),
                Enroll("Alonso", "Composition", Grade.B),
                Enroll("Anand", "Chemistry"),
                Enroll("Anand", "Microeconomics", Grade.B),
                Enroll("Barzdukas", "Chemistry", Grade.B),
                Enroll("Li", "Composition", Grade.B),
                Enroll("Justice", "Literature", Grade.B));
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
