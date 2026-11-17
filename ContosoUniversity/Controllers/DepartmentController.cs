using ContosoUniversity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly SchoolContext _context;

        public DepartmentController(SchoolContext context)
        {
            _context = context;
        }

        // GET: Department
        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments
                .Include(d => d.Administrator)
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync();
            return View(departments);
        }
    }
}
