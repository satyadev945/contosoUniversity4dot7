using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class DepartmentControllerTests
    {
        [Fact]
        public async Task Index_ReturnsDepartmentsOrderedByName()
        {
            using var db = new TestDb().Seed();
            using var ctx = db.CreateContext();
            var view = Assert.IsType<ViewResult>(await new DepartmentController(ctx).Index());
            var model = Assert.IsAssignableFrom<IEnumerable<Department>>(view.Model).ToList();
            Assert.Equal(2, model.Count);
            Assert.Equal("English", model[0].Name);
            Assert.Equal("Mathematics", model[1].Name);
            Assert.NotNull(model[0].Administrator);
        }

        [Fact]
        public async Task Index_NoDepartments_ReturnsEmptyList()
        {
            using var db = new TestDb();
            using var ctx = db.CreateContext();
            var view = Assert.IsType<ViewResult>(await new DepartmentController(ctx).Index());
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<Department>>(view.Model));
        }
    }
}
