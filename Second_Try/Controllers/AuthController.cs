using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Second_Try.Data;
using Second_Try.Models;
using Second_Try.Models.ViewModels;

namespace Second_Try.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect based on role
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin")) return RedirectToAction("Dashboard", "Admin");
                if (User.IsInRole("Employee")) return RedirectToAction("Dashboard", "Employee");
                return RedirectToAction("Dashboard", "Customer");
            }
            return View(new AuthPageViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(AuthPageViewModel pageModel)
        {
            var model = pageModel.Register;

            // Clear login model errors so they don't affect register validation
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Login.")).ToList())
                ModelState.Remove(key);

            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (_context.Customers.Any(c => c.Email == model.Email) ||
                    _context.Employees.Any(e => e.Email == model.Email))
                {
                    ModelState.AddModelError("Register.Email", "This email is already registered.");
                    TempData["ShowRegister"] = true;
                    return View("Login", new AuthPageViewModel { Register = model });
                }

                var customer = new Customer
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    GoogleId = string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // Auto login after registration
                await SignInUser(customer.Email, "Customer", customer.FullName);
                TempData["WelcomeMessage"] = $"Welcome to SRC Travel, {customer.FullName}!";
                return RedirectToAction("Dashboard", "Customer");
            }

            // Validation failed — return to page with register panel open
            TempData["ShowRegister"] = true;
            return View("Login", new AuthPageViewModel { Register = model });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AuthPageViewModel pageModel)
        {
            var model = pageModel.Login;

            // Clear register model errors
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Register.")).ToList())
                ModelState.Remove(key);

            if (ModelState.IsValid)
            {
                // Check Customer
                var customer = _context.Customers.FirstOrDefault(c => c.Email == model.Email);
                if (customer != null && customer.PasswordHash != null &&
                    BCrypt.Net.BCrypt.Verify(model.Password, customer.PasswordHash))
                {
                    await SignInUser(customer.Email, "Customer", customer.FullName);
                    return RedirectToAction("Dashboard", "Customer");
                }

                // Check Employee/Admin
                var employee = _context.Employees.FirstOrDefault(e => e.Email == model.Email);
                if (employee != null && BCrypt.Net.BCrypt.Verify(model.Password, employee.PasswordHash))
                {
                    string roleString = employee.Role == EmployeeRole.Admin ? "Admin" : "Employee";
                    await SignInUser(employee.Email, roleString, employee.FullName);

                    if (employee.Role == EmployeeRole.Admin) return RedirectToAction("Dashboard", "Admin");
                    return RedirectToAction("Dashboard", "Employee");
                }

                ModelState.AddModelError("Login.Email", "Invalid email or password.");
            }

            return View("Login", new AuthPageViewModel { Login = model });
        }

        private async Task SignInUser(string email, string role, string fullName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, email),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        // Logout via GET (navbar link) or POST (form button)
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Logout")]
        public async Task<IActionResult> LogoutPost()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }
    }
}
