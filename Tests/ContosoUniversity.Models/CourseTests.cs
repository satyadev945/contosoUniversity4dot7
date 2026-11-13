using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class CourseTests
    {
        private static List<ValidationResult> Validate(Course c)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(c, new ValidationContext(c), results, true);
            return results;
        }

        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var dept = new Department { Name = "English" };
            var c = new Course { CourseID = 1050, Title = "Chemistry", Credits = 3, DepartmentID = 7, Department = dept,
                Enrollments = new List<Enrollment>(), Instructors = new List<Instructor>() };
            Assert.Equal(1050, c.CourseID);
            Assert.Equal("Chemistry", c.Title);
            Assert.Equal(3, c.Credits);
            Assert.Equal(7, c.DepartmentID);
            Assert.Same(dept, c.Department);
            Assert.Empty(c.Enrollments);
            Assert.Empty(c.Instructors);
        }

        [Fact]
        public void Validation_ValidCourse_Passes()
        {
            Assert.Empty(Validate(new Course { Title = "Calculus", Credits = 4 }));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(6)]
        public void Validation_CreditsOutOfRange_Fails(int credits)
        {
            Assert.Single(Validate(new Course { Title = "Calculus", Credits = credits }));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Validation_CreditsBoundaries_Pass(int credits)
        {
            Assert.Empty(Validate(new Course { Title = "Calculus", Credits = credits }));
        }

        [Theory]
        [InlineData("ab")]
        [InlineData("")]
        public void Validation_TitleTooShort_Fails(string title)
        {
            Assert.NotEmpty(Validate(new Course { Title = title, Credits = 1 }));
        }

        [Fact]
        public void Validation_TitleTooLong_Fails()
        {
            Assert.NotEmpty(Validate(new Course { Title = new string('x', 51), Credits = 1 }));
        }
    }
}
