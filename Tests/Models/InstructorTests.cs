using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
        public void Properties_RoundTrip()
        {
            var office = new OfficeAssignment { Location = "Smith 17" };
            var i = new Instructor
            {
                ID = 4, HireDate = new DateTime(1995, 3, 11), Courses = new List<Course>(), OfficeAssignment = office
            };
            Assert.Equal(4, i.ID);
            Assert.Equal(new DateTime(1995, 3, 11), i.HireDate);
            Assert.Empty(i.Courses);
            Assert.Same(office, i.OfficeAssignment);
        }

        [Fact]
        public void Validation_MissingNames_Fails()
        {
            var results = ValidationHelper.Validate(new Instructor());
            Assert.Contains(results, r => r.MemberNames.Contains("LastName"));
            Assert.Contains(results, r => r.MemberNames.Contains("FirstMidName"));
        }

        [Fact]
        public void Validation_NameTooLong_Fails()
        {
            var results = ValidationHelper.Validate(new Instructor { LastName = new string('a', 51), FirstMidName = "Kim" });
            Assert.Contains(results, r => r.MemberNames.Contains("LastName"));
        }

        [Fact]
        public void Validation_ValidInstructor_Passes()
        {
            Assert.Empty(ValidationHelper.Validate(new Instructor { LastName = "A", FirstMidName = "B" }));
        }
    }
}
