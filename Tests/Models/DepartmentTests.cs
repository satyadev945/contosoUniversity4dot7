using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class DepartmentTests
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var admin = new Instructor { ID = 7 };
            var d = new Department
            {
                DepartmentID = 1, Name = "English", Budget = 123.45m, StartDate = new DateTime(2007, 9, 1),
                InstructorID = 7, Administrator = admin, Courses = new List<Course>()
            };
            Assert.Equal(1, d.DepartmentID);
            Assert.Equal("English", d.Name);
            Assert.Equal(123.45m, d.Budget);
            Assert.Equal(new DateTime(2007, 9, 1), d.StartDate);
            Assert.Equal(7, d.InstructorID);
            Assert.Same(admin, d.Administrator);
            Assert.Empty(d.Courses);
        }

        [Fact]
        public void InstructorID_IsNullableByDefault()
        {
            Assert.Null(new Department().InstructorID);
        }

        [Fact]
        public void Validation_NameTooShort_Fails()
        {
            Assert.Contains(ValidationHelper.Validate(new Department { Name = "ab" }),
                r => r.MemberNames.Contains("Name"));
        }

        [Fact]
        public void Validation_ValidName_Passes()
        {
            Assert.Empty(ValidationHelper.Validate(new Department { Name = "English" }));
        }
    }
}
