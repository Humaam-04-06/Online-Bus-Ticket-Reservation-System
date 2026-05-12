using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Second_Try.Data;

namespace Second_Try.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
