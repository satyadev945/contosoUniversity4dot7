using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace ContosoUniversity.Migrations
{
    public class SchoolContextModelSnapshotTests
    {
        // The snapshot class is internal, so it is instantiated by reflection.
        private static IModel BuildModel()
        {
            var type = typeof(ContosoUniversity.DAL.SchoolContext).Assembly.GetType("ContosoUniversity.Migrations.SchoolContextModelSnapshot");
            Assert.NotNull(type);
            var snapshot = (ModelSnapshot)Activator.CreateInstance(type, nonPublic: true);
            return snapshot.Model;
        }

        [Fact]
        public void Model_ContainsAllEntityTypes()
        {
            var names = BuildModel().GetEntityTypes().Select(e => e.Name).ToList();
            Assert.Contains("ContosoUniversity.Models.Course", names);
            Assert.Contains("ContosoUniversity.Models.Department", names);
            Assert.Contains("ContosoUniversity.Models.Enrollment", names);
            Assert.Contains("ContosoUniversity.Models.Instructor", names);
            Assert.Contains("ContosoUniversity.Models.OfficeAssignment", names);
            Assert.Contains("ContosoUniversity.Models.Student", names);
        }

        [Fact]
        public void Model_CourseHasCourseIDKey()
        {
            var course = BuildModel().FindEntityType("ContosoUniversity.Models.Course");
            Assert.NotNull(course);
            Assert.Equal("CourseID", course.FindPrimaryKey().Properties.Single().Name);
        }
    }
}
