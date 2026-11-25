using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class StudentTests
    {
        [Fact]
        public void FullName_CombinesLastAndFirst()
        {
            var s = new Student { LastName = "Alexander", FirstMidName = "Carson" };
            Assert.Equal("Alexander, Carson", s.FullName);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var s = new Student { ID = 9, EnrollmentDate = new DateTime(2010, 9, 1), Enrollments = new List<Enrollment>() };
            Assert.Equal(9, s.ID);
            Assert.Equal(new DateTime(2010, 9, 1), s.EnrollmentDate);
            Assert.Empty(s.Enrollments);
        }

        [Fact]
        public void Validation_MissingNames_Fails()
        {
            var results = ValidationHelper.Validate(new Student());
            Assert.Contains(results, r => r.MemberNames.Contains("LastName"));
            Assert.Contains(results, r => r.MemberNames.Contains("FirstMidName"));
        }

        [Fact]
        public void Validation_FirstNameTooLong_UsesCustomMessage()
        {
            var results = ValidationHelper.Validate(new Student { LastName = "A", FirstMidName = new string('x', 51) });
            var error = Assert.Single(results);
            Assert.Equal("First Name must be less than 50 Characters", error.ErrorMessage);
        }

        [Fact]
        public void Validation_ValidStudent_Passes()
        {
            Assert.Empty(ValidationHelper.Validate(new Student { LastName = "A", FirstMidName = "B" }));
        }
    }
}
