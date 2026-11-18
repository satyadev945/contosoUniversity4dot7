using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class CourseTests
    {
        private static IList<ValidationResult> Validate(object o)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(o, new ValidationContext(o), results, true);
            return results;
        }

        [Fact]
        public void Constructor_InitializesCollections()
        {
            var c = new Course();
            Assert.NotNull(c.Enrollments);
            Assert.NotNull(c.Instructors);
            Assert.Empty(c.Enrollments);
            Assert.Empty(c.Instructors);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var dept = new Department { Name = "Eng" };
            var c = new Course { CourseID = 10, Title = "Chem", Credits = 3, DepartmentID = 2, Department = dept };
            Assert.Equal(10, c.CourseID);
            Assert.Equal("Chem", c.Title);
            Assert.Equal(3, c.Credits);
            Assert.Equal(2, c.DepartmentID);
            Assert.Same(dept, c.Department);
        }

        [Fact]
        public void Validation_ValidCourse_HasNoErrors()
        {
            Assert.Empty(Validate(new Course { Title = "Calculus", Credits = 4 }));
        }

        [Theory]
        [InlineData("ab")]
        [InlineData("This title is way too long for the fifty character maximum limit")]
        public void Validation_InvalidTitleLength_HasError(string title)
        {
            Assert.NotEmpty(Validate(new Course { Title = title, Credits = 3 }));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(6)]
        public void Validation_CreditsOutOfRange_HasError(int credits)
        {
            Assert.NotEmpty(Validate(new Course { Title = "Valid", Credits = credits }));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Validation_CreditsBoundary_IsValid(int credits)
        {
            Assert.Empty(Validate(new Course { Title = "Valid", Credits = credits }));
        }
    }
}
