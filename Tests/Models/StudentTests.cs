using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class StudentTests
    {
        [Fact]
        public void FullName_CombinesLastAndFirst()
        {
            var s = new Student { LastName = "Alexander", FirstMidName = "Carson" };
            Assert.Equal("Alexander, Carson", s.FullName);
        }

        [Fact]
        public void Constructor_InitializesEnrollments()
        {
            var s = new Student();
            Assert.NotNull(s.Enrollments);
            Assert.Empty(s.Enrollments);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var date = new DateTime(2010, 9, 1);
            var s = new Student { ID = 9, EnrollmentDate = date };
            Assert.Equal(9, s.ID);
            Assert.Equal(date, s.EnrollmentDate);
        }

        [Fact]
        public void Validation_MissingRequired_Fails()
        {
            var s = new Student();
            var results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(s, new ValidationContext(s), results, true));
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void Validation_FirstNameTooLong_UsesCustomMessage()
        {
            var s = new Student { LastName = "L", FirstMidName = new string('f', 51) };
            var results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(s, new ValidationContext(s), results, true));
            Assert.Contains(results, r => r.ErrorMessage == "First Name must be less than 50 Characters");
        }
    }
}
