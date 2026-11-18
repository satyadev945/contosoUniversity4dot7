using ContosoUniversity.ViewModels;
using Xunit;

namespace ContosoUniversity.ViewModels
{
    public class EnrollmentDataGroupTests
    {
        [Fact]
        public void Defaults_AreNullAndZero()
        {
            var g = new EnrollmentDataGroup();
            Assert.Null(g.EnrollmentDate);
            Assert.Equal(0, g.StudentCount);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var d = new DateTime(2012, 9, 1);
            var g = new EnrollmentDataGroup { EnrollmentDate = d, StudentCount = 4 };
            Assert.Equal(d, g.EnrollmentDate);
            Assert.Equal(4, g.StudentCount);
        }
    }
}
