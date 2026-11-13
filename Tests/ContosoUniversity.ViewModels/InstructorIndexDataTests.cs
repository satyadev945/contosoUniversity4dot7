using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using Xunit;

namespace ContosoUniversity.ViewModels
{
    public class InstructorIndexDataTests
    {
        [Fact]
        public void NewInstance_HasNullCollections()
        {
            var d = new InstructorIndexData();
            Assert.Null(d.Instructors);
            Assert.Null(d.Courses);
            Assert.Null(d.Enrollments);
        }

        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var i = new List<Instructor> { new Instructor() };
            var c = new List<Course> { new Course(), new Course() };
            var e = new List<Enrollment>();
            var d = new InstructorIndexData { Instructors = i, Courses = c, Enrollments = e };
            Assert.Single(d.Instructors);
            Assert.Equal(2, d.Courses.Count());
            Assert.Empty(d.Enrollments);
        }
    }
}
