using ContosoUniversity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly SchoolContext _db;

        public DepartmentController(SchoolContext db)
        {
            _db = db;
        }

        // GET: Department
        public async Task<IActionResult> Index()
        {
            var departments = await _db.Departments
                .Include(d => d.Administrator)
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync();
            return View(departments);
        }
    }
}
