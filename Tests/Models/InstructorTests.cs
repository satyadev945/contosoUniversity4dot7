using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class InstructorTests
    {
        [Fact]
        public void FullName_CombinesLastAndFirst()
        {
            var i = new Instructor { LastName = "Abercrombie", FirstMidName = "Kim" };
            Assert.Equal("Abercrombie, Kim", i.FullName);
        }

        [Fact]
        public void Constructor_InitializesCourses()
        {
            var i = new Instructor();
            Assert.NotNull(i.Courses);
            Assert.Empty(i.Courses);
            Assert.Null(i.OfficeAssignment);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var hire = new DateTime(1995, 3, 11);
            var office = new OfficeAssignment { Location = "X" };
            var i = new Instructor { ID = 3, HireDate = hire, OfficeAssignment = office };
            Assert.Equal(3, i.ID);
            Assert.Equal(hire, i.HireDate);
            Assert.Same(office, i.OfficeAssignment);
        }

        [Fact]
        public void Validation_MissingRequired_Fails()
        {
            var i = new Instructor();
            var results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(i, new ValidationContext(i), results, true));
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void Validation_Valid_Passes()
        {
            var i = new Instructor { LastName = "A", FirstMidName = "B" };
            Assert.True(Validator.TryValidateObject(i, new ValidationContext(i), new List<ValidationResult>(), true));
        }
    }
}
