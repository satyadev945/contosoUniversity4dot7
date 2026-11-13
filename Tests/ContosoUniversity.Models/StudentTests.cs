using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class StudentTests
    {
        private static IList<ValidationResult> Validate(object o)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(o, new ValidationContext(o), results, true);
            return results;
        }

        [Fact]
        public void NewStudent_HasDefaultValues()
        {
            var s = new Student();
            Assert.Equal(0, s.ID);
            Assert.Null(s.LastName);
            Assert.Null(s.FirstMidName);
            Assert.Null(s.Enrollments);
            Assert.Equal(default(DateTime), s.EnrollmentDate);
        }

        [Fact]
        public void FullName_CombinesLastAndFirstName()
        {
            var s = new Student { LastName = "Alexander", FirstMidName = "Carson" };
            Assert.Equal("Alexander, Carson", s.FullName);
        }

        [Fact]
        public void FullName_WithNulls_ReturnsSeparatorOnly()
        {
            Assert.Equal(", ", new Student().FullName);
        }

        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var date = new DateTime(2020, 1, 2);
            var enrollments = new List<Enrollment> { new Enrollment() };
            var s = new Student { ID = 4, LastName = "L", FirstMidName = "F", EnrollmentDate = date, Enrollments = enrollments };
            Assert.Equal(4, s.ID);
            Assert.Equal(date, s.EnrollmentDate);
            Assert.Same(enrollments, s.Enrollments);
        }

        [Fact]
        public void Validation_ValidStudent_HasNoErrors()
        {
            var s = new Student { LastName = "Li", FirstMidName = "Yan", EnrollmentDate = DateTime.Today };
            Assert.Empty(Validate(s));
        }

        [Fact]
        public void Validation_MissingRequiredFields_ReturnsErrors()
        {
            var results = Validate(new Student());
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void Validation_FirstNameTooLong_UsesCustomMessage()
        {
            var s = new Student { LastName = "Li", FirstMidName = new string('a', 51) };
            var results = Validate(s);
            Assert.Contains(results, r => r.ErrorMessage == "First Name must be less than 50 Characters");
        }

        [Fact]
        public void Validation_LastNameTooLong_ReturnsError()
        {
            var s = new Student { LastName = new string('a', 51), FirstMidName = "A" };
            Assert.Single(Validate(s));
        }
    }
}
