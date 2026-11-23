using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class ModelTests
    {
        private static IList<ValidationResult> Validate(object o)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(o, new ValidationContext(o), results, true);
            return results;
        }

        [Fact]
        public void Course_Defaults()
        {
            var c = new Course();
            Assert.Null(c.Title);
            Assert.Null(c.Department);
            Assert.Empty(c.Enrollments);
            Assert.Empty(c.Instructors);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(5, true)]
        [InlineData(6, false)]
        [InlineData(-1, false)]
        public void Course_Credits_Range(int credits, bool valid)
        {
            var c = new Course { CourseID = 1, Title = "Chem", Credits = credits };
            Assert.Equal(valid, Validate(c).Count == 0);
        }

        [Theory]
        [InlineData("ab", false)]
        [InlineData("abc", true)]
        public void Course_Title_Length(string title, bool valid)
        {
            var c = new Course { Title = title, Credits = 1 };
            Assert.Equal(valid, Validate(c).Count == 0);
        }

        [Fact]
        public void Department_SetProperties()
        {
            var d = new Department { DepartmentID = 3, Name = "Eng", Budget = 12.5m, StartDate = new DateTime(2020, 1, 1), InstructorID = 4 };
            Assert.Equal(3, d.DepartmentID);
            Assert.Equal("Eng", d.Name);
            Assert.Equal(12.5m, d.Budget);
            Assert.Equal(4, d.InstructorID);
            Assert.Null(d.Administrator);
            Assert.Empty(d.Courses);
        }

        [Fact]
        public void Enrollment_GradeNullable()
        {
            var e = new Enrollment { EnrollmentID = 1, CourseID = 2, StudentID = 3 };
            Assert.Null(e.Grade);
            e.Grade = Grade.F;
            Assert.Equal(Grade.F, e.Grade);
            Assert.Null(e.Course);
            Assert.Null(e.Student);
        }

        [Fact]
        public void Grade_EnumValues()
        {
            Assert.Equal(5, Enum.GetValues<Grade>().Length);
            Assert.Equal(0, (int)Grade.A);
        }

        [Fact]
        public void Instructor_FullName()
        {
            var i = new Instructor { LastName = "Kapoor", FirstMidName = "Candace" };
            Assert.Equal("Kapoor, Candace", i.FullName);
            Assert.Empty(i.Courses);
            Assert.Null(i.OfficeAssignment);
        }

        [Fact]
        public void Instructor_RequiredFields()
        {
            var i = new Instructor { LastName = "", FirstMidName = "" };
            Assert.Equal(2, Validate(i).Count);
        }

        [Fact]
        public void Instructor_Valid()
        {
            var i = new Instructor { LastName = "A", FirstMidName = "B", HireDate = DateTime.Today };
            Assert.Empty(Validate(i));
        }

        [Fact]
        public void Student_FullName()
        {
            var s = new Student { LastName = "Li", FirstMidName = "Yan" };
            Assert.Equal("Li, Yan", s.FullName);
            Assert.Empty(s.Enrollments);
        }

        [Fact]
        public void Student_Validation_TooLongFirstName()
        {
            var s = new Student { LastName = "Li", FirstMidName = new string('x', 51) };
            Assert.Single(Validate(s));
        }

        [Fact]
        public void Student_Validation_RequiredMissing()
        {
            var s = new Student { LastName = "", FirstMidName = "" };
            Assert.Equal(2, Validate(s).Count);
        }

        [Fact]
        public void OfficeAssignment_Properties()
        {
            var o = new OfficeAssignment { InstructorID = 2, Location = "Smith" };
            Assert.Equal(2, o.InstructorID);
            Assert.Equal("Smith", o.Location);
            Assert.Null(o.Instructor);
        }

        [Fact]
        public void OfficeAssignment_LocationTooLong()
        {
            var o = new OfficeAssignment { Location = new string('x', 51) };
            Assert.Single(Validate(o));
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("abc", true)]
        public void ErrorViewModel_ShowRequestId(string? id, bool expected)
        {
            var m = new ErrorViewModel { RequestId = id };
            Assert.Equal(expected, m.ShowRequestId);
        }
    }
}
