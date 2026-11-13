using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class InstructorTests
    {
        [Fact]
        public void FullName_CombinesNames()
        {
            var i = new Instructor { LastName = "Kapoor", FirstMidName = "Candace" };
            Assert.Equal("Kapoor, Candace", i.FullName);
        }

        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var hire = new DateTime(1995, 3, 11);
            var office = new OfficeAssignment { Location = "Smith 17" };
            var courses = new List<Course>();
            var i = new Instructor { ID = 2, HireDate = hire, OfficeAssignment = office, Courses = courses };
            Assert.Equal(2, i.ID);
            Assert.Equal(hire, i.HireDate);
            Assert.Same(office, i.OfficeAssignment);
            Assert.Same(courses, i.Courses);
        }

        [Fact]
        public void Validation_MissingNames_ReturnsTwoErrors()
        {
            var i = new Instructor();
            var results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(i, new ValidationContext(i), results, true));
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void Validation_ValidInstructor_Passes()
        {
            var i = new Instructor { LastName = "A", FirstMidName = "B" };
            Assert.True(Validator.TryValidateObject(i, new ValidationContext(i), new List<ValidationResult>(), true));
        }
    }
}
