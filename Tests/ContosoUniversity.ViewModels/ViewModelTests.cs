using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using Xunit;

namespace ContosoUniversity.ViewModels
{
    public class ViewModelTests
    {
        [Fact]
        public void InstructorIndexData_Defaults()
        {
            var d = new InstructorIndexData();
            Assert.NotNull(d.Instructors);
            Assert.Empty(d.Instructors);
            Assert.Null(d.Courses);
            Assert.Null(d.Enrollments);
        }

        [Fact]
        public void InstructorIndexData_SetProperties()
        {
            var d = new InstructorIndexData
            {
                Instructors = new[] { new Instructor { ID = 1 } },
                Courses = new[] { new Course { CourseID = 2 } },
                Enrollments = new[] { new Enrollment { EnrollmentID = 3 } }
            };
            Assert.Single(d.Instructors);
            Assert.Single(d.Courses!);
            Assert.Single(d.Enrollments!);
        }

        [Fact]
        public void EnrollmentDataGroup_Properties()
        {
            var g = new EnrollmentDataGroup { EnrollmentDate = new DateTime(2020, 1, 1), StudentCount = 4 };
            Assert.Equal(new DateTime(2020, 1, 1), g.EnrollmentDate);
            Assert.Equal(4, g.StudentCount);
            Assert.Null(new EnrollmentDataGroup().EnrollmentDate);
        }
    }
}
