using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class OfficeAssignmentTests
    {
        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var instr = new Instructor();
            var o = new OfficeAssignment { InstructorID = 5, Location = "Smith 17", Instructor = instr };
            Assert.Equal(5, o.InstructorID);
            Assert.Equal("Smith 17", o.Location);
            Assert.Same(instr, o.Instructor);
        }

        [Fact]
        public void Validation_LocationTooLong_Fails()
        {
            var o = new OfficeAssignment { Location = new string('a', 51) };
            Assert.False(Validator.TryValidateObject(o, new ValidationContext(o), new List<ValidationResult>(), true));
        }

        [Fact]
        public void Validation_LocationValid_Passes()
        {
            var o = new OfficeAssignment { Location = "Gowan 27" };
            Assert.True(Validator.TryValidateObject(o, new ValidationContext(o), new List<ValidationResult>(), true));
        }
    }
}
