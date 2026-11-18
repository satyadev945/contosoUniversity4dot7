using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class DepartmentTests
    {
        [Fact]
        public void Constructor_InitializesCourses()
        {
            var d = new Department();
            Assert.NotNull(d.Courses);
            Assert.Empty(d.Courses);
            Assert.Null(d.InstructorID);
            Assert.Null(d.Administrator);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var admin = new Instructor { LastName = "A", FirstMidName = "B" };
            var start = new DateTime(2007, 9, 1);
            var d = new Department { DepartmentID = 4, Name = "Math", Budget = 100.5m, StartDate = start, InstructorID = 7, Administrator = admin };
            Assert.Equal(4, d.DepartmentID);
            Assert.Equal("Math", d.Name);
            Assert.Equal(100.5m, d.Budget);
            Assert.Equal(start, d.StartDate);
            Assert.Equal(7, d.InstructorID);
            Assert.Same(admin, d.Administrator);
        }

        [Theory]
        [InlineData("ab", false)]
        [InlineData("abc", true)]
        public void Validation_NameLength(string name, bool valid)
        {
            var d = new Department { Name = name };
            var results = new List<ValidationResult>();
            Assert.Equal(valid, Validator.TryValidateObject(d, new ValidationContext(d), results, true));
        }
    }
}
