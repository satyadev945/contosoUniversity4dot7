using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.DAL
{
    /// <summary>
    /// Idempotent seed data (ported from the EF6 migrations Configuration.Seed method).
    /// </summary>
    public static class DbInitializer
    {
        public static async Task SeedAsync(SchoolContext context)
        {
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
            for (int i = 0; i < students.Count; i++)
            {
                var lastName = students[i].LastName;
                var existing = await context.Students.FirstOrDefaultAsync(p => p.LastName == lastName);
                if (existing == null)
                {
                    context.Students.Add(students[i]);
                }
                else
                {
                    students[i] = existing;
                }
            }
            await context.SaveChangesAsync();

            var instructors = new List<Instructor>
            {
                new Instructor { FirstMidName = "Kim",     LastName = "Abercrombie", HireDate = DateTime.Parse("1995-03-11") },
                new Instructor { FirstMidName = "Fadi",    LastName = "Fakhouri",    HireDate = DateTime.Parse("2002-07-06") },
                new Instructor { FirstMidName = "Roger",   LastName = "Harui",       HireDate = DateTime.Parse("1998-07-01") },
                new Instructor { FirstMidName = "Candace", LastName = "Kapoor",      HireDate = DateTime.Parse("2001-01-15") },
                new Instructor { FirstMidName = "Roger",   LastName = "Zheng",       HireDate = DateTime.Parse("2004-02-12") }
            };
            for (int i = 0; i < instructors.Count; i++)
            {
                var name = instructors[i].LastName;
                var existing = await context.Instructors.FirstOrDefaultAsync(p => p.LastName == name);
                if (existing == null)
                {
                    context.Instructors.Add(instructors[i]);
                }
                else
                {
                    instructors[i] = existing;
                }
            }
            await context.SaveChangesAsync();

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
            for (int i = 0; i < departments.Count; i++)
            {
                var name = departments[i].Name;
                var existing = await context.Departments.FirstOrDefaultAsync(p => p.Name == name);
                if (existing == null)
                {
                    context.Departments.Add(departments[i]);
                }
                else
                {
                    departments[i] = existing;
                }
            }
            await context.SaveChangesAsync();

            var courseSeeds = new[]
            {
                new Course { CourseID = 1050, Title = "Chemistry",      Credits = 3, DepartmentID = departments.Single(s => s.Name == "Engineering").DepartmentID },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = departments.Single(s => s.Name == "Economics").DepartmentID },
                new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, DepartmentID = departments.Single(s => s.Name == "Economics").DepartmentID },
                new Course { CourseID = 1045, Title = "Calculus",       Credits = 4, DepartmentID = departments.Single(s => s.Name == "Mathematics").DepartmentID },
                new Course { CourseID = 3141, Title = "Trigonometry",   Credits = 4, DepartmentID = departments.Single(s => s.Name == "Mathematics").DepartmentID },
                new Course { CourseID = 2021, Title = "Composition",    Credits = 3, DepartmentID = departments.Single(s => s.Name == "English").DepartmentID },
                new Course { CourseID = 2042, Title = "Literature",     Credits = 4, DepartmentID = departments.Single(s => s.Name == "English").DepartmentID },
            };
            var courses = new List<Course>();
            foreach (var c in courseSeeds)
            {
                var existing = await context.Courses.Include(x => x.Instructors).FirstOrDefaultAsync(p => p.CourseID == c.CourseID);
                if (existing == null)
                {
                    c.Instructors = new List<Instructor>();
                    context.Courses.Add(c);
                    courses.Add(c);
                }
                else
                {
                    courses.Add(existing);
                }
            }
            await context.SaveChangesAsync();

            var officeAssignments = new[]
            {
                new OfficeAssignment { InstructorID = instructors.Single(i => i.LastName == "Fakhouri").ID, Location = "Smith 17" },
                new OfficeAssignment { InstructorID = instructors.Single(i => i.LastName == "Harui").ID,    Location = "Gowan 27" },
                new OfficeAssignment { InstructorID = instructors.Single(i => i.LastName == "Kapoor").ID,   Location = "Thompson 304" },
            };
            foreach (var o in officeAssignments)
            {
                if (!await context.OfficeAssignments.AnyAsync(p => p.InstructorID == o.InstructorID))
                {
                    context.OfficeAssignments.Add(o);
                }
            }
            await context.SaveChangesAsync();

            AddInstructorToCourse(courses, instructors, "Chemistry", "Kapoor");
            AddInstructorToCourse(courses, instructors, "Chemistry", "Harui");
            AddInstructorToCourse(courses, instructors, "Microeconomics", "Zheng");
            AddInstructorToCourse(courses, instructors, "Macroeconomics", "Zheng");
            AddInstructorToCourse(courses, instructors, "Calculus", "Fakhouri");
            AddInstructorToCourse(courses, instructors, "Trigonometry", "Harui");
            AddInstructorToCourse(courses, instructors, "Composition", "Abercrombie");
            AddInstructorToCourse(courses, instructors, "Literature", "Abercrombie");
            await context.SaveChangesAsync();

            var enrollments = new List<Enrollment>
            {
                new Enrollment { StudentID = students.Single(s => s.LastName == "Alexander").ID, CourseID = courses.Single(c => c.Title == "Chemistry").CourseID, Grade = Grade.A },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Alexander").ID, CourseID = courses.Single(c => c.Title == "Microeconomics").CourseID, Grade = Grade.C },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Alexander").ID, CourseID = courses.Single(c => c.Title == "Macroeconomics").CourseID, Grade = Grade.B },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Alonso").ID,    CourseID = courses.Single(c => c.Title == "Calculus").CourseID, Grade = Grade.B },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Alonso").ID,    CourseID = courses.Single(c => c.Title == "Trigonometry").CourseID, Grade = Grade.B },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Alonso").ID,    CourseID = courses.Single(c => c.Title == "Composition").CourseID, Grade = Grade.B },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Anand").ID,     CourseID = courses.Single(c => c.Title == "Chemistry").CourseID },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Anand").ID,     CourseID = courses.Single(c => c.Title == "Microeconomics").CourseID, Grade = Grade.B },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Barzdukas").ID, CourseID = courses.Single(c => c.Title == "Chemistry").CourseID, Grade = Grade.B },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Li").ID,        CourseID = courses.Single(c => c.Title == "Composition").CourseID, Grade = Grade.B },
                new Enrollment { StudentID = students.Single(s => s.LastName == "Justice").ID,   CourseID = courses.Single(c => c.Title == "Literature").CourseID, Grade = Grade.B }
            };

            foreach (var e in enrollments)
            {
                bool exists = await context.Enrollments.AnyAsync(s => s.StudentID == e.StudentID && s.CourseID == e.CourseID);
                if (!exists)
                {
                    context.Enrollments.Add(e);
                }
            }
            await context.SaveChangesAsync();
        }

        private static void AddInstructorToCourse(List<Course> courses, List<Instructor> instructors, string courseTitle, string instructorName)
        {
            var course = courses.Single(c => c.Title == courseTitle);
            course.Instructors ??= new List<Instructor>();
            if (!course.Instructors.Any(i => i.LastName == instructorName))
            {
                course.Instructors.Add(instructors.Single(i => i.LastName == instructorName));
            }
        }
    }
}
