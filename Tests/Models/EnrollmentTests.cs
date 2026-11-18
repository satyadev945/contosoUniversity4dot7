using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class EnrollmentTests
    {
        [Fact]
        public void Grade_DefaultsToNull()
        {
            Assert.Null(new Enrollment().Grade);
        }

        [Theory]
        [InlineData(Grade.A)]
        [InlineData(Grade.B)]
        [InlineData(Grade.C)]
        [InlineData(Grade.D)]
        [InlineData(Grade.F)]
        public void Grade_CanBeAssigned(Grade grade)
        {
            var e = new Enrollment { Grade = grade };
            Assert.Equal(grade, e.Grade);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var course = new Course();
            var student = new Student();
            var e = new Enrollment { EnrollmentID = 1, CourseID = 2, StudentID = 3, Course = course, Student = student };
            Assert.Equal(1, e.EnrollmentID);
            Assert.Equal(2, e.CourseID);
            Assert.Equal(3, e.StudentID);
            Assert.Same(course, e.Course);
            Assert.Same(student, e.Student);
        }

        [Fact]
        public void GradeEnum_HasFiveValues()
        {
            Assert.Equal(5, Enum.GetValues<Grade>().Length);
        }
    }
}
