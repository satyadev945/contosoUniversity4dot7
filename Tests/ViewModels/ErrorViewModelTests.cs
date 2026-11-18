using ContosoUniversity.ViewModels;
using Xunit;

namespace ContosoUniversity.ViewModels
{
    public class ErrorViewModelTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("abc", true)]
        public void ShowRequestId_ReflectsRequestId(string requestId, bool expected)
        {
            var vm = new ErrorViewModel { RequestId = requestId };
            Assert.Equal(expected, vm.ShowRequestId);
            Assert.Equal(requestId, vm.RequestId);
        }
    }
}
