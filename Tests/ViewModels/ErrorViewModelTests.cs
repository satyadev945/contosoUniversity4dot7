using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.ViewModels
{
    public class ErrorViewModelTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("abc", true)]
        public void ShowRequestId_DependsOnRequestId(string requestId, bool expected)
        {
            var vm = new ErrorViewModel { RequestId = requestId };
            Assert.Equal(requestId, vm.RequestId);
            Assert.Equal(expected, vm.ShowRequestId);
        }
    }
}
