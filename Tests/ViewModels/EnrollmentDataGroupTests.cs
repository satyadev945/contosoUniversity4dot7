using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.ViewModels
{
    public class EnrollmentDataGroupTests
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var date = new DateTime(2012, 9, 1);
            var g = new EnrollmentDataGroup { EnrollmentDate = date, StudentCount = 4 };
            Assert.Equal(date, g.EnrollmentDate);
            Assert.Equal(4, g.StudentCount);
        }

        [Fact]
        public void Defaults()
        {
            var g = new EnrollmentDataGroup();
            Assert.Null(g.EnrollmentDate);
            Assert.Equal(0, g.StudentCount);
        }
    }
}
