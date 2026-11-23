using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class OfficeAssignmentTests
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var instructor = new Instructor { ID = 3 };
            var oa = new OfficeAssignment { InstructorID = 3, Location = "Gowan 27", Instructor = instructor };
            Assert.Equal(3, oa.InstructorID);
            Assert.Equal("Gowan 27", oa.Location);
            Assert.Same(instructor, oa.Instructor);
        }

        [Fact]
        public void Defaults_AreNull()
        {
            var oa = new OfficeAssignment();
            Assert.Null(oa.Location);
            Assert.Null(oa.Instructor);
            Assert.Equal(0, oa.InstructorID);
        }

        [Fact]
        public void Validation_LocationTooLong_Fails()
        {
            var oa = new OfficeAssignment { Location = new string('x', 51) };
            Assert.False(Validator.TryValidateObject(oa, new ValidationContext(oa), new List<ValidationResult>(), true));
        }

        [Fact]
        public void Validation_LocationOk_Passes()
        {
            var oa = new OfficeAssignment { Location = new string('x', 50) };
            Assert.True(Validator.TryValidateObject(oa, new ValidationContext(oa), new List<ValidationResult>(), true));
        }
    }
}
