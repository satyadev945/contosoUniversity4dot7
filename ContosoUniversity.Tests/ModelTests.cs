using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Tests
{
    public class ModelTests
    {
        [Fact]
        public void Model_UsesSingularTableNamesAndJoinTable()
        {
            using var db = new TestDatabase();
            var model = db.Context.Model;

            Assert.Equal("Student", model.FindEntityType(typeof(Student))!.GetTableName());
            Assert.Equal("Course", model.FindEntityType(typeof(Course))!.GetTableName());
            Assert.Equal("Instructor", model.FindEntityType(typeof(Instructor))!.GetTableName());
            Assert.Contains(model.GetEntityTypes(), e => e.GetTableName() == "CourseInstructor");
        }

        [Fact]
        public void Student_FirstMidName_MapsToFirstNameColumn()
        {
            using var db = new TestDatabase();
            var entity = db.Context.Model.FindEntityType(typeof(Student))!;
            var property = entity.FindProperty(nameof(Student.FirstMidName))!;

            Assert.Equal("FirstName", property.GetColumnName());
        }

        [Fact]
        public void FullName_IsNotMapped()
        {
            using var db = new TestDatabase();

            Assert.Null(db.Context.Model.FindEntityType(typeof(Student))!.FindProperty(nameof(Student.FullName)));
            Assert.Null(db.Context.Model.FindEntityType(typeof(Instructor))!.FindProperty(nameof(Instructor.FullName)));
        }

        [Fact]
        public async Task CourseInstructor_ManyToMany_RoundTrips()
        {
            using var db = new TestDatabase();
            var department = new Department { Name = "English", StartDate = DateTime.Today };
            var instructor = new Instructor { LastName = "Abercrombie", FirstMidName = "Kim", HireDate = DateTime.Today };
            db.Context.Departments.Add(department);
            db.Context.Instructors.Add(instructor);
            await db.Context.SaveChangesAsync();

            db.Context.Courses.Add(new Course
            {
                CourseID = 2021,
                Title = "Composition",
                Credits = 3,
                DepartmentID = department.DepartmentID,
                Instructors = new List<Instructor> { instructor }
            });
            await db.Context.SaveChangesAsync();

            var loaded = await db.Context.Courses.Include(c => c.Instructors).SingleAsync();
            Assert.Single(loaded.Instructors!);
        }
    }
}
