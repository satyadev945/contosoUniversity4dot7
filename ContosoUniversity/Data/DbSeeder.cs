using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// Idempotent seed data (replaces the EF6 Migrations Configuration.Seed / AddOrUpdate logic and the
    /// DropCreateDatabaseIfModelChanges initializer). Existing rows are matched on their natural keys and
    /// are never duplicated.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(SchoolContext context, CancellationToken cancellationToken = default)
        {
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

            foreach (var student in students)
            {
                if (!await context.Students.AnyAsync(s => s.LastName == student.LastName, cancellationToken))
                {
                    context.Students.Add(student);
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            var instructors = new[]
            {
                new Instructor { FirstMidName = "Kim",     LastName = "Abercrombie", HireDate = DateTime.Parse("1995-03-11") },
                new Instructor { FirstMidName = "Fadi",    LastName = "Fakhouri",    HireDate = DateTime.Parse("2002-07-06") },
                new Instructor { FirstMidName = "Roger",   LastName = "Harui",       HireDate = DateTime.Parse("1998-07-01") },
                new Instructor { FirstMidName = "Candace", LastName = "Kapoor",      HireDate = DateTime.Parse("2001-01-15") },
                new Instructor { FirstMidName = "Roger",   LastName = "Zheng",       HireDate = DateTime.Parse("2004-02-12") }
            };

            foreach (var instructor in instructors)
            {
                if (!await context.Instructors.AnyAsync(i => i.LastName == instructor.LastName, cancellationToken))
                {
                    context.Instructors.Add(instructor);
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            var instructorByName = await context.Instructors.ToDictionaryAsync(i => i.LastName, cancellationToken);

            var departments = new[]
            {
                new Department { Name = "English",     Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructorByName["Abercrombie"].ID },
                new Department { Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructorByName["Fakhouri"].ID },
                new Department { Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructorByName["Harui"].ID },
                new Department { Name = "Economics",   Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructorByName["Kapoor"].ID }
            };

            foreach (var department in departments)
            {
                if (!await context.Departments.AnyAsync(d => d.Name == department.Name, cancellationToken))
                {
                    context.Departments.Add(department);
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            var departmentByName = await context.Departments.ToDictionaryAsync(d => d.Name, cancellationToken);

            var courses = new[]
            {
                new Course { CourseID = 1050, Title = "Chemistry",      Credits = 3, DepartmentID = departmentByName["Engineering"].DepartmentID },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = departmentByName["Economics"].DepartmentID },
                new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, DepartmentID = departmentByName["Economics"].DepartmentID },
                new Course { CourseID = 1045, Title = "Calculus",       Credits = 4, DepartmentID = departmentByName["Mathematics"].DepartmentID },
                new Course { CourseID = 3141, Title = "Trigonometry",   Credits = 4, DepartmentID = departmentByName["Mathematics"].DepartmentID },
                new Course { CourseID = 2021, Title = "Composition",    Credits = 3, DepartmentID = departmentByName["English"].DepartmentID },
                new Course { CourseID = 2042, Title = "Literature",     Credits = 4, DepartmentID = departmentByName["English"].DepartmentID }
            };

            foreach (var course in courses)
            {
                if (!await context.Courses.AnyAsync(c => c.CourseID == course.CourseID, cancellationToken))
                {
                    context.Courses.Add(course);
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            var officeAssignments = new[]
            {
                new OfficeAssignment { InstructorID = instructorByName["Fakhouri"].ID, Location = "Smith 17" },
                new OfficeAssignment { InstructorID = instructorByName["Harui"].ID,    Location = "Gowan 27" },
                new OfficeAssignment { InstructorID = instructorByName["Kapoor"].ID,   Location = "Thompson 304" }
            };

            foreach (var assignment in officeAssignments)
            {
                if (!await context.OfficeAssignments.AnyAsync(o => o.InstructorID == assignment.InstructorID, cancellationToken))
                {
                    context.OfficeAssignments.Add(assignment);
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            await EnsureCourseInstructorAsync(context, "Chemistry", "Kapoor", cancellationToken);
            await EnsureCourseInstructorAsync(context, "Chemistry", "Harui", cancellationToken);
            await EnsureCourseInstructorAsync(context, "Microeconomics", "Zheng", cancellationToken);
            await EnsureCourseInstructorAsync(context, "Macroeconomics", "Zheng", cancellationToken);
            await EnsureCourseInstructorAsync(context, "Calculus", "Fakhouri", cancellationToken);
            await EnsureCourseInstructorAsync(context, "Trigonometry", "Harui", cancellationToken);
            await EnsureCourseInstructorAsync(context, "Composition", "Abercrombie", cancellationToken);
            await EnsureCourseInstructorAsync(context, "Literature", "Abercrombie", cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var studentByName = await context.Students.ToDictionaryAsync(s => s.LastName, cancellationToken);
            var courseByTitle = await context.Courses.ToDictionaryAsync(c => c.Title, cancellationToken);

            var enrollments = new[]
            {
                new Enrollment { StudentID = studentByName["Alexander"].ID, CourseID = courseByTitle["Chemistry"].CourseID,       Grade = Grade.A },
                new Enrollment { StudentID = studentByName["Alexander"].ID, CourseID = courseByTitle["Microeconomics"].CourseID,  Grade = Grade.C },
                new Enrollment { StudentID = studentByName["Alexander"].ID, CourseID = courseByTitle["Macroeconomics"].CourseID,  Grade = Grade.B },
                new Enrollment { StudentID = studentByName["Alonso"].ID,    CourseID = courseByTitle["Calculus"].CourseID,        Grade = Grade.B },
                new Enrollment { StudentID = studentByName["Alonso"].ID,    CourseID = courseByTitle["Trigonometry"].CourseID,    Grade = Grade.B },
                new Enrollment { StudentID = studentByName["Alonso"].ID,    CourseID = courseByTitle["Composition"].CourseID,     Grade = Grade.B },
                new Enrollment { StudentID = studentByName["Anand"].ID,     CourseID = courseByTitle["Chemistry"].CourseID },
                new Enrollment { StudentID = studentByName["Anand"].ID,     CourseID = courseByTitle["Microeconomics"].CourseID,  Grade = Grade.B },
                new Enrollment { StudentID = studentByName["Barzdukas"].ID, CourseID = courseByTitle["Chemistry"].CourseID,       Grade = Grade.B },
                new Enrollment { StudentID = studentByName["Li"].ID,        CourseID = courseByTitle["Composition"].CourseID,     Grade = Grade.B },
                new Enrollment { StudentID = studentByName["Justice"].ID,   CourseID = courseByTitle["Literature"].CourseID,      Grade = Grade.B }
            };

            foreach (var enrollment in enrollments)
            {
                var exists = await context.Enrollments.AnyAsync(
                    e => e.StudentID == enrollment.StudentID && e.CourseID == enrollment.CourseID,
                    cancellationToken);
                if (!exists)
                {
                    context.Enrollments.Add(enrollment);
                }
            }
            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task EnsureCourseInstructorAsync(
            SchoolContext context,
            string courseTitle,
            string instructorLastName,
            CancellationToken cancellationToken)
        {
            var course = await context.Courses
                .Include(c => c.Instructors)
                .SingleOrDefaultAsync(c => c.Title == courseTitle, cancellationToken);
            if (course == null)
            {
                return;
            }

            course.Instructors ??= new List<Instructor>();
            if (course.Instructors.Any(i => i.LastName == instructorLastName))
            {
                return;
            }

            var instructor = await context.Instructors
                .SingleOrDefaultAsync(i => i.LastName == instructorLastName, cancellationToken);
            if (instructor != null)
            {
                course.Instructors.Add(instructor);
            }
        }
    }
}
