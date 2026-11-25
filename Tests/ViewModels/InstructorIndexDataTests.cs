using System;
using System.Collections.Generic;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.ViewModels
{
    public class InstructorIndexDataTests
    {
        [Fact]
        public void NewInstance_CollectionsAreNull()
        {
            var d = new InstructorIndexData();
            Assert.Null(d.Instructors);
            Assert.Null(d.Courses);
            Assert.Null(d.Enrollments);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var d = new InstructorIndexData
            {
                Instructors = new List<Instructor> { new Instructor() },
                Courses = new List<Course> { new Course(), new Course() },
                Enrollments = new List<Enrollment>()
            };
            Assert.Single(d.Instructors);
            Assert.Equal(2, System.Linq.Enumerable.Count(d.Courses));
            Assert.Empty(d.Enrollments);
        }
    }
}
