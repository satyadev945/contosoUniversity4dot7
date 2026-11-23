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
            var admin = new Instructor { ID = 4 };
            var start = new DateTime(2007, 9, 1);
            var d = new Department { DepartmentID = 1, Name = "English", Budget = 350000m, StartDate = start, InstructorID = 4, Administrator = admin };
            Assert.Equal(1, d.DepartmentID);
            Assert.Equal("English", d.Name);
            Assert.Equal(350000m, d.Budget);
            Assert.Equal(start, d.StartDate);
            Assert.Equal(4, d.InstructorID);
            Assert.Same(admin, d.Administrator);
        }

        [Theory]
        [InlineData("ab", false)]
        [InlineData("abc", true)]
        [InlineData("English", true)]
        public void Validation_NameLength(string name, bool valid)
        {
            var d = new Department { Name = name };
            var results = new List<ValidationResult>();
            var ok = Validator.TryValidateObject(d, new ValidationContext(d), results, true);
            Assert.Equal(valid, ok);
        }
    }
}
