using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using ContosoUniversity.ViewModels;
using Xunit;

namespace ContosoUniversity.ViewModels
{
    public class PaginatedListTests
    {
        [Fact]
        public void Constructor_SetsPagingProperties()
        {
            var list = new PaginatedList<int>(new List<int> { 1, 2, 3 }, 10, 2, 3);
            Assert.Equal(2, list.PageIndex);
            Assert.Equal(10, list.TotalCount);
            Assert.Equal(4, list.TotalPages);
            Assert.Equal(3, list.Count);
            Assert.True(list.HasPreviousPage);
            Assert.True(list.HasNextPage);
        }

        [Fact]
        public void FirstPage_HasNoPrevious()
        {
            var list = new PaginatedList<int>(new List<int> { 1 }, 5, 1, 1);
            Assert.False(list.HasPreviousPage);
            Assert.True(list.HasNextPage);
        }

        [Fact]
        public void LastPage_HasNoNext()
        {
            var list = new PaginatedList<int>(new List<int> { 5 }, 5, 5, 1);
            Assert.True(list.HasPreviousPage);
            Assert.False(list.HasNextPage);
        }

        [Fact]
        public void EmptyList_HasZeroPages()
        {
            var list = new PaginatedList<int>(new List<int>(), 0, 1, 3);
            Assert.Equal(0, list.TotalPages);
            Assert.False(list.HasNextPage);
            Assert.Empty(list);
        }

        [Theory]
        [InlineData(7, 3, 3)]
        [InlineData(6, 3, 2)]
        [InlineData(1, 3, 1)]
        public void TotalPages_RoundsUp(int count, int pageSize, int expected)
        {
            var list = new PaginatedList<int>(new List<int>(), count, 1, pageSize);
            Assert.Equal(expected, list.TotalPages);
        }

        [Fact]
        public async Task CreateAsync_ReturnsRequestedPage()
        {
            using var db = new TestDb().Seed();
            using var ctx = db.CreateContext();
            var result = await PaginatedList<Student>.CreateAsync(ctx.Students.OrderBy(s => s.LastName), 2, 2);
            Assert.Equal(2, result.PageIndex);
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(3, result.TotalPages);
            Assert.Equal(2, result.Count);
            Assert.Equal("Anand", result[0].LastName);
        }

        [Fact]
        public async Task CreateAsync_PageIndexBelowOne_ClampsToOne()
        {
            using var db = new TestDb().Seed();
            using var ctx = db.CreateContext();
            var result = await PaginatedList<Student>.CreateAsync(ctx.Students.OrderBy(s => s.LastName), -3, 2);
            Assert.Equal(1, result.PageIndex);
            Assert.Equal("Alexander", result[0].LastName);
        }

        [Fact]
        public async Task CreateAsync_PageBeyondEnd_ReturnsEmpty()
        {
            using var db = new TestDb().Seed();
            using var ctx = db.CreateContext();
            var result = await PaginatedList<Student>.CreateAsync(ctx.Students.OrderBy(s => s.LastName), 10, 2);
            Assert.Empty(result);
            Assert.Equal(5, result.TotalCount);
        }
    }
}
