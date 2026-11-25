using System;
using System.Collections.Generic;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.ViewModels
{
    public class EnrollmentDataGroupTests
    {
        [Fact]
        public void NewInstance_HasDefaults()
        {
            var g = new EnrollmentDataGroup();
            Assert.Null(g.EnrollmentDate);
            Assert.Equal(0, g.StudentCount);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var g = new EnrollmentDataGroup { EnrollmentDate = new DateTime(2012, 9, 1), StudentCount = 4 };
            Assert.Equal(new DateTime(2012, 9, 1), g.EnrollmentDate);
            Assert.Equal(4, g.StudentCount);
        }
    }
}
