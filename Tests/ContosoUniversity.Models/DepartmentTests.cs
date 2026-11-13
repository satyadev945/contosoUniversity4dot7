using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class DepartmentTests
    {
        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var admin = new Instructor();
            var d = new Department
            {
                DepartmentID = 1, Name = "Math", Budget = 100000.5m, StartDate = new DateTime(2007, 9, 1),
                InstructorID = 3, Administrator = admin, Courses = new List<Course>()
            };
            Assert.Equal(1, d.DepartmentID);
            Assert.Equal("Math", d.Name);
            Assert.Equal(100000.5m, d.Budget);
            Assert.Equal(new DateTime(2007, 9, 1), d.StartDate);
            Assert.Equal(3, d.InstructorID);
            Assert.Same(admin, d.Administrator);
            Assert.Empty(d.Courses);
        }

        [Fact]
        public void InstructorID_IsNullableByDefault()
        {
            Assert.Null(new Department().InstructorID);
        }

        [Theory]
        [InlineData("ab", false)]
        [InlineData("abc", true)]
        [InlineData("English", true)]
        public void Validation_NameLength(string name, bool valid)
        {
            var d = new Department { Name = name };
            Assert.Equal(valid, Validator.TryValidateObject(d, new ValidationContext(d), new List<ValidationResult>(), true));
        }
    }
}
