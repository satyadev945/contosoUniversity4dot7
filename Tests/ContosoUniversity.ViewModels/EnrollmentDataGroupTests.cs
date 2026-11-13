using ContosoUniversity.ViewModels;
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
        public void Properties_CanBeSetAndRead()
        {
            var date = new DateTime(2012, 9, 1);
            var g = new EnrollmentDataGroup { EnrollmentDate = date, StudentCount = 3 };
            Assert.Equal(date, g.EnrollmentDate);
            Assert.Equal(3, g.StudentCount);
        }
    }
}
