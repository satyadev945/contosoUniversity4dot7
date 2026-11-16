using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Data
{
    /// <summary>
    /// Idempotent seeding that replaces the EF6 Migrations Configuration.Seed (AddOrUpdate) and
    /// the DropCreateDatabaseIfModelChanges initializer. It only inserts rows that do not yet exist
    /// and never drops data.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task SeedAsync(SchoolContext context, CancellationToken cancellationToken = default)
        {
            var studentSeed = new[]
            {
                ("Carson", "Alexander", "2010-09-01"),
                ("Meredith", "Alonso", "2012-09-01"),
                ("Arturo", "Anand", "2013-09-01"),
                ("Gytis", "Barzdukas", "2012-09-01"),
                ("Yan", "Li", "2012-09-01"),
                ("Peggy", "Justice", "2011-09-01"),
                ("Laura", "Norman", "2013-09-01"),
                ("Nino", "Olivetto", "2005-09-01")
            };

            foreach (var (first, last, date) in studentSeed)
            {
                if (!await context.Students.AnyAsync(s => s.LastName == last, cancellationToken))
                {
                    context.Students.Add(new Student { FirstMidName = first, LastName = last, EnrollmentDate = DateTime.Parse(date) });
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            var instructorSeed = new[]
            {
                ("Kim", "Abercrombie", "1995-03-11"),
                ("Fadi", "Fakhouri", "2002-07-06"),
                ("Roger", "Harui", "1998-07-01"),
                ("Candace", "Kapoor", "2001-01-15"),
                ("Roger", "Zheng", "2004-02-12")
            };

            foreach (var (first, last, date) in instructorSeed)
            {
                if (!await context.Instructors.AnyAsync(i => i.LastName == last, cancellationToken))
                {
                    context.Instructors.Add(new Instructor { FirstMidName = first, LastName = last, HireDate = DateTime.Parse(date) });
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            var instructors = await context.Instructors.ToListAsync(cancellationToken);

            var departmentSeed = new[]
            {
                ("English", 350000m, "Abercrombie"),
                ("Mathematics", 100000m, "Fakhouri"),
                ("Engineering", 350000m, "Harui"),
                ("Economics", 100000m, "Kapoor")
            };

            foreach (var (name, budget, admin) in departmentSeed)
            {
                if (!await context.Departments.AnyAsync(d => d.Name == name, cancellationToken))
                {
                    context.Departments.Add(new Department
                    {
                        Name = name,
                        Budget = budget,
                        StartDate = DateTime.Parse("2007-09-01"),
                        InstructorID = instructors.Single(i => i.LastName == admin).ID
                    });
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            var departments = await context.Departments.ToListAsync(cancellationToken);

            var courseSeed = new[]
            {
                (1050, "Chemistry", 3, "Engineering"),
                (4022, "Microeconomics", 3, "Economics"),
                (4041, "Macroeconomics", 3, "Economics"),
                (1045, "Calculus", 4, "Mathematics"),
                (3141, "Trigonometry", 4, "Mathematics"),
                (2021, "Composition", 3, "English"),
                (2042, "Literature", 4, "English")
            };

            foreach (var (id, title, credits, dept) in courseSeed)
            {
                if (!await context.Courses.AnyAsync(c => c.CourseID == id, cancellationToken))
                {
                    context.Courses.Add(new Course
                    {
                        CourseID = id,
                        Title = title,
                        Credits = credits,
                        DepartmentID = departments.Single(d => d.Name == dept).DepartmentID
                    });
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            var officeSeed = new[]
            {
                ("Fakhouri", "Smith 17"),
                ("Harui", "Gowan 27"),
                ("Kapoor", "Thompson 304")
            };

            foreach (var (last, location) in officeSeed)
            {
                var instructorId = instructors.Single(i => i.LastName == last).ID;
                if (!await context.OfficeAssignments.AnyAsync(o => o.InstructorID == instructorId, cancellationToken))
                {
                    context.OfficeAssignments.Add(new OfficeAssignment { InstructorID = instructorId, Location = location });
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            var assignments = new[]
            {
                ("Chemistry", "Kapoor"), ("Chemistry", "Harui"),
                ("Microeconomics", "Zheng"), ("Macroeconomics", "Zheng"),
                ("Calculus", "Fakhouri"), ("Trigonometry", "Harui"),
                ("Composition", "Abercrombie"), ("Literature", "Abercrombie")
            };

            foreach (var (courseTitle, instructorName) in assignments)
            {
                var course = await context.Courses
                    .Include(c => c.Instructors)
                    .SingleOrDefaultAsync(c => c.Title == courseTitle, cancellationToken);
                if (course != null && !course.Instructors.Any(i => i.LastName == instructorName))
                {
                    course.Instructors.Add(instructors.Single(i => i.LastName == instructorName));
                }
            }
            await context.SaveChangesAsync(cancellationToken);

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

            foreach (var e in enrollmentSeed)
            {
                var studentId = students.Single(s => s.LastName == e.Student).ID;
                var courseId = courses.Single(c => c.Title == e.Course).CourseID;
                if (!await context.Enrollments.AnyAsync(x => x.StudentID == studentId && x.CourseID == courseId, cancellationToken))
                {
                    context.Enrollments.Add(new Enrollment { StudentID = studentId, CourseID = courseId, Grade = e.Grade });
                }
            }
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
