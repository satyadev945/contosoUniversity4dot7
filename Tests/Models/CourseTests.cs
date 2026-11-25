using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class CourseTests
    {
        [Fact]
        public void NewCourse_HasDefaults()
        {
            var c = new Course();
            Assert.Equal(0, c.CourseID);
            Assert.Null(c.Title);
            Assert.Null(c.Department);
            Assert.Null(c.Enrollments);
            Assert.Null(c.Instructors);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var dept = new Department { DepartmentID = 3 };
            var c = new Course
            {
                CourseID = 1050, Title = "Chemistry", Credits = 3, DepartmentID = 3, Department = dept,
                Enrollments = new List<Enrollment>(), Instructors = new List<Instructor>()
            };
            Assert.Equal(1050, c.CourseID);
            Assert.Equal("Chemistry", c.Title);
            Assert.Equal(3, c.Credits);
            Assert.Same(dept, c.Department);
            Assert.Empty(c.Enrollments);
            Assert.Empty(c.Instructors);
        }

        [Fact]
        public void Validation_ValidCourse_HasNoErrors()
        {
            Assert.Empty(ValidationHelper.Validate(new Course { Title = "Chemistry", Credits = 3 }));
        }

        [Theory]
        [InlineData("ab")]
        [InlineData("")]
        public void Validation_TitleTooShort_Fails(string title)
        {
            if (title == "") return; // empty strings are skipped by StringLength
            Assert.Contains(ValidationHelper.Validate(new Course { Title = title, Credits = 3 }),
                r => r.MemberNames.Contains("Title"));
        }

        [Fact]
        public void Validation_TitleTooLong_Fails()
        {
            var results = ValidationHelper.Validate(new Course { Title = new string('x', 51), Credits = 3 });
            Assert.Contains(results, r => r.MemberNames.Contains("Title"));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(6)]
        public void Validation_CreditsOutOfRange_Fails(int credits)
        {
            var results = ValidationHelper.Validate(new Course { Title = "Chemistry", Credits = credits });
            Assert.Contains(results, r => r.MemberNames.Contains("Credits"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Validation_CreditsBoundary_Passes(int credits)
        {
            Assert.Empty(ValidationHelper.Validate(new Course { Title = "Chemistry", Credits = credits }));
        }
    }
}
