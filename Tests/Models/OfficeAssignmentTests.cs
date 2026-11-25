using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class OfficeAssignmentTests
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var instructor = new Instructor();
            var o = new OfficeAssignment { InstructorID = 3, Location = "Gowan 27", Instructor = instructor };
            Assert.Equal(3, o.InstructorID);
            Assert.Equal("Gowan 27", o.Location);
            Assert.Same(instructor, o.Instructor);
        }

        [Fact]
        public void Validation_LocationTooLong_Fails()
        {
            Assert.Contains(ValidationHelper.Validate(new OfficeAssignment { Location = new string('x', 51) }),
                r => r.MemberNames.Contains("Location"));
        }

        [Fact]
        public void Validation_LocationValid_Passes()
        {
            Assert.Empty(ValidationHelper.Validate(new OfficeAssignment { Location = "Smith 17" }));
        }
    }
}
