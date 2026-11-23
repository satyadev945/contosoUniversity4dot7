using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class StudentTests
    {
        [Fact]
        public void Constructor_InitializesEnrollments()
        {
            var s = new Student();
            Assert.NotNull(s.Enrollments);
            Assert.Empty(s.Enrollments);
        }

        [Fact]
        public void FullName_CombinesLastAndFirst()
        {
            var s = new Student { LastName = "Alexander", FirstMidName = "Carson" };
            Assert.Equal("Alexander, Carson", s.FullName);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var date = new DateTime(2010, 9, 1);
            var s = new Student { ID = 5, EnrollmentDate = date };
            s.Enrollments.Add(new Enrollment { EnrollmentID = 1 });
            Assert.Equal(5, s.ID);
            Assert.Equal(date, s.EnrollmentDate);
            Assert.Single(s.Enrollments);
        }

        [Fact]
        public void Validation_MissingRequired_Fails()
        {
            var s = new Student();
            var results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(s, new ValidationContext(s), results, true));
            Assert.True(results.Count >= 2);
        }

        [Fact]
        public void Validation_FirstNameTooLong_UsesCustomMessage()
        {
            var s = new Student { LastName = "A", FirstMidName = new string('x', 51) };
            var results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(s, new ValidationContext(s), results, true));
            Assert.Contains(results, r => r.ErrorMessage == "First Name must be less than 50 Characters");
        }

        [Fact]
        public void Validation_Valid_Passes()
        {
            var s = new Student { LastName = "Li", FirstMidName = "Yan" };
            Assert.True(Validator.TryValidateObject(s, new ValidationContext(s), new List<ValidationResult>(), true));
        }
    }
}
