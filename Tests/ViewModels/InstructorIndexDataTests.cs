using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using Xunit;

namespace ContosoUniversity.ViewModels
{
    public class InstructorIndexDataTests
    {
        [Fact]
        public void Defaults_AreNull()
        {
            var d = new InstructorIndexData();
            Assert.Null(d.Instructors);
            Assert.Null(d.Courses);
            Assert.Null(d.Enrollments);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var instructors = new List<Instructor> { new Instructor() };
            var courses = new List<Course> { new Course(), new Course() };
            var enrollments = new List<Enrollment>();
            var d = new InstructorIndexData { Instructors = instructors, Courses = courses, Enrollments = enrollments };
            Assert.Same(instructors, d.Instructors);
            Assert.Equal(2, d.Courses.Count());
            Assert.Same(enrollments, d.Enrollments);
        }
    }
}
