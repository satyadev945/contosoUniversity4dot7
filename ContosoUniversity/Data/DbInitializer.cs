using ContosoUniversity.Models;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// Idempotent seeder. Replaces the EF6 SchoolInitializer / migrations Configuration.Seed.
    /// </summary>
    public static class DbInitializer
    {
        public static void Initialize(SchoolContext context)
        {
            if (context.Students.Any())
            {
                return;
            }

            var students = new[]
            {
                new Student { FirstMidName = "Carson", LastName = "Alexander", EnrollmentDate = DateTime.Parse("2005-09-01") },
                new Student { FirstMidName = "Meredith", LastName = "Alonso", EnrollmentDate = DateTime.Parse("2002-09-01") },
                new Student { FirstMidName = "Arturo", LastName = "Anand", EnrollmentDate = DateTime.Parse("2003-09-01") },
                new Student { FirstMidName = "Gytis", LastName = "Barzdukas", EnrollmentDate = DateTime.Parse("2002-09-01") },
                new Student { FirstMidName = "Yan", LastName = "Li", EnrollmentDate = DateTime.Parse("2002-09-01") },
                new Student { FirstMidName = "Peggy", LastName = "Justice", EnrollmentDate = DateTime.Parse("2001-09-01") },
                new Student { FirstMidName = "Laura", LastName = "Norman", EnrollmentDate = DateTime.Parse("2003-09-01") },
                new Student { FirstMidName = "Nino", LastName = "Olivetto", EnrollmentDate = DateTime.Parse("2005-09-01") }
            };
            context.Students.AddRange(students);

            var instructors = new[]
            {
                new Instructor { FirstMidName = "Kim", LastName = "Abercrombie", HireDate = DateTime.Parse("1995-03-11") },
                new Instructor { FirstMidName = "Fadi", LastName = "Fakhouri", HireDate = DateTime.Parse("2002-07-06") },
                new Instructor { FirstMidName = "Roger", LastName = "Harui", HireDate = DateTime.Parse("1998-07-01") },
                new Instructor { FirstMidName = "Candace", LastName = "Kapoor", HireDate = DateTime.Parse("2001-01-15") },
                new Instructor { FirstMidName = "Roger", LastName = "Zheng", HireDate = DateTime.Parse("2004-02-12") }
            };
            context.Instructors.AddRange(instructors);

            context.OfficeAssignments.AddRange(
                new OfficeAssignment { Instructor = instructors[1], Location = "Smith 17" },
                new OfficeAssignment { Instructor = instructors[2], Location = "Gowan 27" },
                new OfficeAssignment { Instructor = instructors[3], Location = "Thompson 304" });

            var departments = new[]
            {
                new Department { Name = "English", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), Administrator = instructors[0] },
                new Department { Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), Administrator = instructors[1] },
                new Department { Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), Administrator = instructors[2] },
                new Department { Name = "Economics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), Administrator = instructors[3] }
            };
            context.Departments.AddRange(departments);

            var courses = new[]
            {
                new Course { CourseID = 1050, Title = "Chemistry", Credits = 3, Department = departments[2], Instructors = new List<Instructor> { instructors[0], instructors[1] } },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, Department = departments[3], Instructors = new List<Instructor> { instructors[2] } },
                new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, Department = departments[3], Instructors = new List<Instructor> { instructors[3] } },
                new Course { CourseID = 1045, Title = "Calculus", Credits = 4, Department = departments[1], Instructors = new List<Instructor> { instructors[3] } },
                new Course { CourseID = 3141, Title = "Trigonometry", Credits = 4, Department = departments[1], Instructors = new List<Instructor> { instructors[4] } },
                new Course { CourseID = 2021, Title = "Composition", Credits = 3, Department = departments[0], Instructors = new List<Instructor> { instructors[0] } },
                new Course { CourseID = 2042, Title = "Literature", Credits = 4, Department = departments[0], Instructors = new List<Instructor> { instructors[1] } }
            };
            context.Courses.AddRange(courses);

            Course C(int id) => courses.Single(c => c.CourseID == id);

            context.Enrollments.AddRange(
                new Enrollment { Student = students[0], Course = C(1050), Grade = Grade.A },
                new Enrollment { Student = students[0], Course = C(4022), Grade = Grade.C },
                new Enrollment { Student = students[0], Course = C(4041), Grade = Grade.B },
                new Enrollment { Student = students[1], Course = C(1045), Grade = Grade.B },
                new Enrollment { Student = students[1], Course = C(3141), Grade = Grade.F },
                new Enrollment { Student = students[1], Course = C(2021), Grade = Grade.F },
                new Enrollment { Student = students[2], Course = C(1050) },
                new Enrollment { Student = students[3], Course = C(1050) },
                new Enrollment { Student = students[3], Course = C(4022), Grade = Grade.F },
                new Enrollment { Student = students[4], Course = C(4041), Grade = Grade.C },
                new Enrollment { Student = students[5], Course = C(1045) },
                new Enrollment { Student = students[6], Course = C(3141), Grade = Grade.A });

            context.SaveChanges();
        }
    }
}
