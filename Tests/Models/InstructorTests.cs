using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class InstructorTests
    {
        [Fact]
        public void Constructor_InitializesCourses()
        {
            var i = new Instructor();
            Assert.NotNull(i.Courses);
            Assert.Empty(i.Courses);
            Assert.Null(i.OfficeAssignment);
        }

        [Fact]
        public void FullName_CombinesLastAndFirst()
        {
            var i = new Instructor { LastName = "Abercrombie", FirstMidName = "Kim" };
            Assert.Equal("Abercrombie, Kim", i.FullName);
        }

        [Fact]
        public void FullName_WithNulls_ReturnsSeparatorOnly()
        {
            Assert.Equal(", ", new Instructor().FullName);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var oa = new OfficeAssignment { InstructorID = 1, Location = "Smith 17" };
            var hire = new DateTime(1995, 3, 11);
            var i = new Instructor { ID = 1, HireDate = hire, OfficeAssignment = oa };
            Assert.Equal(1, i.ID);
            Assert.Equal(hire, i.HireDate);
            Assert.Same(oa, i.OfficeAssignment);
        }

        [Fact]
        public void Validation_MissingRequiredFields_HasErrors()
        {
            var i = new Instructor();
            var results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(i, new ValidationContext(i), results, true));
            Assert.True(results.Count >= 2);
        }

        [Fact]
        public void Validation_ValidInstructor_Passes()
        {
            var i = new Instructor { LastName = "Fakhouri", FirstMidName = "Fadi" };
            Assert.True(Validator.TryValidateObject(i, new ValidationContext(i), new List<ValidationResult>(), true));
        }
    }
}
