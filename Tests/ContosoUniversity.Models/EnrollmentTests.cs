using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class EnrollmentTests
    {
        [Fact]
        public void NewEnrollment_GradeIsNull()
        {
            Assert.Null(new Enrollment().Grade);
        }

        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var course = new Course();
            var student = new Student();
            var e = new Enrollment { EnrollmentID = 1, CourseID = 2, StudentID = 3, Grade = Grade.B, Course = course, Student = student };
            Assert.Equal(1, e.EnrollmentID);
            Assert.Equal(2, e.CourseID);
            Assert.Equal(3, e.StudentID);
            Assert.Equal(Grade.B, e.Grade);
            Assert.Same(course, e.Course);
            Assert.Same(student, e.Student);
        }

        [Theory]
        [InlineData(Grade.A, 0)]
        [InlineData(Grade.B, 1)]
        [InlineData(Grade.C, 2)]
        [InlineData(Grade.D, 3)]
        [InlineData(Grade.F, 4)]
        public void GradeEnum_HasExpectedValues(Grade grade, int value)
        {
            Assert.Equal(value, (int)grade);
        }

        [Fact]
        public void GradeEnum_HasFiveValues()
        {
            Assert.Equal(5, Enum.GetValues<Grade>().Length);
        }
    }
}
