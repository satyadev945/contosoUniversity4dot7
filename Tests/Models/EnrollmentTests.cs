using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class EnrollmentTests
    {
        [Fact]
        public void NewEnrollment_GradeIsNull()
        {
            Assert.Null(new Enrollment().Grade);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var course = new Course(); var student = new Student();
            var e = new Enrollment { EnrollmentID = 1, CourseID = 2, StudentID = 3, Grade = Grade.B, Course = course, Student = student };
            Assert.Equal(1, e.EnrollmentID);
            Assert.Equal(2, e.CourseID);
            Assert.Equal(3, e.StudentID);
            Assert.Equal(Grade.B, e.Grade);
            Assert.Same(course, e.Course);
            Assert.Same(student, e.Student);
        }

        [Fact]
        public void Grade_EnumValues_AreOrdered()
        {
            Assert.Equal(new[] { Grade.A, Grade.B, Grade.C, Grade.D, Grade.F },
                Enum.GetValues<Grade>());
            Assert.Equal(0, (int)Grade.A);
            Assert.Equal(4, (int)Grade.F);
        }
    }
}
