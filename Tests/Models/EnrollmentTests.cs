using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class EnrollmentTests
    {
        [Fact]
        public void Defaults_GradeIsNull()
        {
            var e = new Enrollment();
            Assert.Null(e.Grade);
            Assert.Null(e.Course);
            Assert.Null(e.Student);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var course = new Course { CourseID = 1 };
            var student = new Student { ID = 2 };
            var e = new Enrollment { EnrollmentID = 9, CourseID = 1, StudentID = 2, Grade = Grade.B, Course = course, Student = student };
            Assert.Equal(9, e.EnrollmentID);
            Assert.Equal(1, e.CourseID);
            Assert.Equal(2, e.StudentID);
            Assert.Equal(Grade.B, e.Grade);
            Assert.Same(course, e.Course);
            Assert.Same(student, e.Student);
        }

        [Fact]
        public void GradeEnum_HasExpectedValues()
        {
            Assert.Equal(new[] { Grade.A, Grade.B, Grade.C, Grade.D, Grade.F }, Enum.GetValues<Grade>());
            Assert.Equal(0, (int)Grade.A);
            Assert.Equal(4, (int)Grade.F);
        }
    }
}
