using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.DAL
{
    /// <summary>
    /// Idempotent database seeder (replaces the EF6 DropCreateDatabaseIfModelChanges initializer
    /// and the DbMigrationsConfiguration.Seed / AddOrUpdate logic).
    /// </summary>
    public static class SchoolInitializer
    {
        public static async Task SeedAsync(SchoolContext context, CancellationToken cancellationToken = default)
        {
            // Students
            var studentSeed = new[]
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
            foreach (var student in studentSeed)
            {
                if (!await context.Students.AnyAsync(s => s.LastName == student.LastName, cancellationToken))
                {
                    context.Students.Add(student);
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            // Instructors
            var instructorSeed = new[]
            {
                new Instructor { FirstMidName = "Kim",     LastName = "Abercrombie", HireDate = DateTime.Parse("1995-03-11") },
                new Instructor { FirstMidName = "Fadi",    LastName = "Fakhouri",    HireDate = DateTime.Parse("2002-07-06") },
                new Instructor { FirstMidName = "Roger",   LastName = "Harui",       HireDate = DateTime.Parse("1998-07-01") },
                new Instructor { FirstMidName = "Candace", LastName = "Kapoor",      HireDate = DateTime.Parse("2001-01-15") },
                new Instructor { FirstMidName = "Roger",   LastName = "Zheng",       HireDate = DateTime.Parse("2004-02-12") }
            };
            foreach (var instructor in instructorSeed)
            {
                if (!await context.Instructors.AnyAsync(i => i.LastName == instructor.LastName, cancellationToken))
                {
                    context.Instructors.Add(instructor);
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            var instructors = await context.Instructors.ToListAsync(cancellationToken);

            // Departments
            var departmentSeed = new[]
            {
                new Department { Name = "English",     Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single(i => i.LastName == "Abercrombie").ID },
                new Department { Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single(i => i.LastName == "Fakhouri").ID },
                new Department { Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single(i => i.LastName == "Harui").ID },
                new Department { Name = "Economics",   Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single(i => i.LastName == "Kapoor").ID }
            };
            foreach (var department in departmentSeed)
            {
                if (!await context.Departments.AnyAsync(d => d.Name == department.Name, cancellationToken))
                {
                    context.Departments.Add(department);
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            var departments = await context.Departments.ToListAsync(cancellationToken);

            // Courses
            var courseSeed = new[]
            {
                new Course { CourseID = 1050, Title = "Chemistry",      Credits = 3, DepartmentID = departments.Single(d => d.Name == "Engineering").DepartmentID },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = departments.Single(d => d.Name == "Economics").DepartmentID },
                new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, DepartmentID = departments.Single(d => d.Name == "Economics").DepartmentID },
                new Course { CourseID = 1045, Title = "Calculus",       Credits = 4, DepartmentID = departments.Single(d => d.Name == "Mathematics").DepartmentID },
                new Course { CourseID = 3141, Title = "Trigonometry",   Credits = 4, DepartmentID = departments.Single(d => d.Name == "Mathematics").DepartmentID },
                new Course { CourseID = 2021, Title = "Composition",    Credits = 3, DepartmentID = departments.Single(d => d.Name == "English").DepartmentID },
                new Course { CourseID = 2042, Title = "Literature",     Credits = 4, DepartmentID = departments.Single(d => d.Name == "English").DepartmentID }
            };
            foreach (var course in courseSeed)
            {
                if (!await context.Courses.AnyAsync(c => c.CourseID == course.CourseID, cancellationToken))
                {
                    context.Courses.Add(course);
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            // Office assignments
            var officeSeed = new[]
            {
                ("Fakhouri", "Smith 17"),
                ("Harui", "Gowan 27"),
                ("Kapoor", "Thompson 304")
            };
            foreach (var (lastName, location) in officeSeed)
            {
                var instructorId = instructors.Single(i => i.LastName == lastName).ID;
                if (!await context.OfficeAssignments.AnyAsync(o => o.InstructorID == instructorId, cancellationToken))
                {
                    context.OfficeAssignments.Add(new OfficeAssignment { InstructorID = instructorId, Location = location });
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            // Course <-> Instructor assignments
            var courseInstructorSeed = new[]
            {
                ("Chemistry", "Kapoor"), ("Chemistry", "Harui"),
                ("Microeconomics", "Zheng"), ("Macroeconomics", "Zheng"),
                ("Calculus", "Fakhouri"), ("Trigonometry", "Harui"),
                ("Composition", "Abercrombie"), ("Literature", "Abercrombie")
            };
            var coursesWithInstructors = await context.Courses.Include(c => c.Instructors).ToListAsync(cancellationToken);
            foreach (var (courseTitle, instructorName) in courseInstructorSeed)
            {
                var course = coursesWithInstructors.Single(c => c.Title == courseTitle);
                course.Instructors ??= new List<Instructor>();
                if (!course.Instructors.Any(i => i.LastName == instructorName))
                {
                    course.Instructors.Add(instructors.Single(i => i.LastName == instructorName));
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            // Enrollments
            var students = await context.Students.ToListAsync(cancellationToken);
            var courses = await context.Courses.ToListAsync(cancellationToken);
            var enrollmentSeed = new (string Student, string Course, Grade? Grade)[]
            {
                ("Alexander", "Chemistry", Grade.A),
                ("Alexander", "Microeconomics", Grade.C),
                ("Alexander", "Macroeconomics", Grade.B),
                ("Alonso", "Calculus", Grade.B),
                ("Alonso", "Trigonometry", Grade.B),
                ("Alonso", "Composition", Grade.B),
                ("Anand", "Chemistry", null),
                ("Anand", "Microeconomics", Grade.B),
                ("Barzdukas", "Chemistry", Grade.B),
                ("Li", "Composition", Grade.B),
                ("Justice", "Literature", Grade.B)
            };
            foreach (var (studentName, courseTitle, grade) in enrollmentSeed)
            {
                var studentId = students.First(s => s.LastName == studentName).ID;
                var courseId = courses.Single(c => c.Title == courseTitle).CourseID;
                if (!await context.Enrollments.AnyAsync(e => e.StudentID == studentId && e.CourseID == courseId, cancellationToken))
                {
                    context.Enrollments.Add(new Enrollment { StudentID = studentId, CourseID = courseId, Grade = grade });
                }
            }
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
