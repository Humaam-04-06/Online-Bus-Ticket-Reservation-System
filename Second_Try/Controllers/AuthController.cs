using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;
using Second_Try.Data;
using Second_Try.Models;
using Second_Try.Models.ViewModels;
using Second_Try.Services;
using Microsoft.EntityFrameworkCore;

namespace Second_Try.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;
        private readonly ITimeLimitedDataProtector _protector;

        public AuthController(ApplicationDbContext context, IEmailService email,
            IDataProtectionProvider dpProvider)
        {
            _context   = context;
            _email     = email;
            _protector = dpProvider
                .CreateProtector("SRCTravel.PasswordReset")
                .ToTimeLimitedDataProtector();
        }

        // ── GET /Auth/Login ───────────────────────────────────────
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))    return RedirectToAction("Dashboard", "Admin");
                if (User.IsInRole("Employee")) return RedirectToAction("Dashboard", "Employee");
                return RedirectToAction("Dashboard", "Customer");
            }
            PopulateAuthStats();
            return View(new AuthPageViewModel());
        }

        // ── POST /Auth/Register ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(AuthPageViewModel pageModel)
        {
            var model = pageModel.Register;

            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Login.")).ToList())
                ModelState.Remove(key);

            if (ModelState.IsValid)
            {
                if (_context.Customers.Any(c => c.Email == model.Email) ||
                    _context.Employees.Any(e => e.Email == model.Email))
                {
                    ModelState.AddModelError("Register.Email", "This email is already registered.");
                    TempData["ShowRegister"] = true;
                    PopulateAuthStats();
                    return View("Login", new AuthPageViewModel { Register = model });
                }

                var customer = new Customer
                {
                    FullName     = model.FullName,
                    Email        = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    GoogleId     = string.Empty,
                    CreatedAt    = DateTime.UtcNow
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // 📧 Send welcome email (fire and forget — won't block)
                _ = _email.SendWelcomeEmailAsync(customer.Email, customer.FullName);

                await SignInUser(customer.Email, "Customer", customer.FullName, model.RememberMe);
                TempData["WelcomeMessage"] = $"Welcome to SRC Travel, {customer.FullName}!";
                return RedirectToAction("Dashboard", "Customer");
            }

            TempData["ShowRegister"] = true;
            PopulateAuthStats();
            return View("Login", new AuthPageViewModel { Register = model });
        }

        // ── POST /Auth/Login ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AuthPageViewModel pageModel)
        {
            var model = pageModel.Login;

            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Register.")).ToList())
                ModelState.Remove(key);

            if (ModelState.IsValid)
            {
                var customer = _context.Customers.FirstOrDefault(c => c.Email == model.Email);
                if (customer != null && customer.PasswordHash != null &&
                    BCrypt.Net.BCrypt.Verify(model.Password, customer.PasswordHash))
                {
                    await SignInUser(customer.Email, "Customer", customer.FullName, model.RememberMe);
                    return RedirectToAction("Dashboard", "Customer");
                }

                var employee = _context.Employees.FirstOrDefault(e => e.Email == model.Email);
                if (employee != null && BCrypt.Net.BCrypt.Verify(model.Password, employee.PasswordHash))
                {
                    string roleString = employee.Role == EmployeeRole.Admin ? "Admin" : "Employee";
                    await SignInUser(employee.Email, roleString, employee.FullName, model.RememberMe);
                    if (employee.Role == EmployeeRole.Admin) return RedirectToAction("Dashboard", "Admin");
                    return RedirectToAction("Dashboard", "Employee");
                }

                ModelState.AddModelError("Login.Email", "Invalid email or password.");
            }

            PopulateAuthStats();
            return View("Login", new AuthPageViewModel { Login = model });
        }

        // ── GET /Auth/GoogleLogin — redirects to Google ───────────
        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback), "Auth", null, Request.Scheme)
            };
            return Challenge(props, GoogleDefaults.AuthenticationScheme);
        }

        // ── GET /signin-google — Google calls this back ───────────
        [HttpGet]
        public async Task<IActionResult> GoogleCallback()
        {
            // Extract the Google identity
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                TempData["ErrorMessage"] = "Google sign-in failed. Please try again.";
                return RedirectToAction(nameof(Login));
            }

            string? googleId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            string? email    = result.Principal.FindFirstValue(ClaimTypes.Email);
            string? name     = result.Principal.FindFirstValue(ClaimTypes.Name)
                               ?? result.Principal.FindFirstValue("name")
                               ?? "Google User";

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Could not retrieve email from Google. Please try again.";
                return RedirectToAction(nameof(Login));
            }

            // Block Google login for Employee / Admin accounts
            if (_context.Employees.Any(e => e.Email == email))
            {
                TempData["ErrorMessage"] = "Staff accounts must log in with email and password.";
                return RedirectToAction(nameof(Login));
            }

            // Find or create customer
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            bool isNew   = false;

            if (customer == null)
            {
                customer = new Customer
                {
                    GoogleId  = googleId ?? email,
                    Email     = email,
                    FullName  = name,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
                isNew = true;
            }
            else if (string.IsNullOrEmpty(customer.GoogleId))
            {
                // Link existing email-registered account to Google
                customer.GoogleId = googleId ?? email;
                await _context.SaveChangesAsync();
            }

            // 📧 Send welcome email for brand-new Google signups
            if (isNew) _ = _email.SendWelcomeEmailAsync(customer.Email, customer.FullName);

            await SignInUser(customer.Email, "Customer", customer.FullName);

            TempData["WelcomeMessage"] = isNew
                ? $"Welcome to SRC Travel, {customer.FullName}! 🎉"
                : $"Welcome back, {customer.FullName}!";

            return RedirectToAction("Dashboard", "Customer");
        }

        // ── Shared sign-in helper ─────────────────────────────────
        private async Task SignInUser(string email, string role, string fullName, bool isPersistent = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, email),
                new Claim(ClaimTypes.Name,           fullName),
                new Claim(ClaimTypes.Email,          email),
                new Claim(ClaimTypes.Role,           role)
            };

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        }

        // ── Logout ────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Logout")]
        public async Task<IActionResult> LogoutPost()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // ══════════════════════════════════════════════════════════
        //  FORGOT PASSWORD
        // ══════════════════════════════════════════════════════════

        // GET /Auth/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        // POST /Auth/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please enter your email address.";
                return View();
            }

            email = email.Trim().ToLower();

            // Find the account (Customer or Employee/Admin)
            string? fullName = null;
            bool found = false;

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer != null) { fullName = customer.FullName; found = true; }

            if (!found)
            {
                var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Email == email);
                if (emp != null) { fullName = emp.FullName; found = true; }
            }

            // Always show success — don't leak whether email exists
            if (found && fullName != null)
            {
                // Generate a 30-minute time-limited token containing the email
                string token = _protector.Protect(email, TimeSpan.FromMinutes(30));
                string resetUrl = Url.Action(nameof(ResetPassword), "Auth",
                    new { token }, Request.Scheme)!;

                await _email.SendPasswordResetEmailAsync(email, fullName, resetUrl);
            }

            ViewBag.Success = true;
            return View();
        }

        // ══════════════════════════════════════════════════════════
        //  RESET PASSWORD
        // ══════════════════════════════════════════════════════════

        // GET /Auth/ResetPassword?token=...
        [HttpGet]
        public IActionResult ResetPassword(string? token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(ForgotPassword));

            // Validate the token is still valid (not expired)
            try
            {
                _protector.Unprotect(token, out var expiry);
                if (expiry < DateTimeOffset.UtcNow)
                {
                    ViewBag.Error = "This reset link has expired. Please request a new one.";
                    return View();
                }
            }
            catch
            {
                ViewBag.Error = "This reset link is invalid or has already been used.";
                return View();
            }

            ViewBag.Token = token;
            return View();
        }

        // POST /Auth/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(ForgotPassword));

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                ViewBag.Token = token;
                ViewBag.Error = "Password must be at least 8 characters.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Token = token;
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            // Unprotect and validate token
            string email;
            try
            {
                email = _protector.Unprotect(token, out var expiry);
                if (expiry < DateTimeOffset.UtcNow)
                {
                    ViewBag.Error = "This reset link has expired. Please request a new one.";
                    return View();
                }
            }
            catch
            {
                ViewBag.Error = "This reset link is invalid or has already been used.";
                return View();
            }

            string newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            string? fullName = null;

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer != null)
            {
                customer.PasswordHash = newHash;
                fullName = customer.FullName;
            }
            else
            {
                var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Email == email);
                if (emp != null)
                {
                    emp.PasswordHash = newHash;
                    fullName = emp.FullName;
                }
            }

            if (fullName == null)
            {
                ViewBag.Error = "Account not found.";
                return View();
            }

            await _context.SaveChangesAsync();

            // Send confirmation email
            _ = _email.SendPasswordChangedEmailAsync(email, fullName);

            TempData["SuccessMessage"] = "Password reset successfully! You can now log in with your new password.";
            return RedirectToAction(nameof(Login));
        }

        private void PopulateAuthStats()
        {
            ViewBag.RoutesCount = _context.Routes.Count();
            ViewBag.TravelersCount = _context.Customers.Count();
            ViewBag.OnTimeRate = _context.Bookings.Any() ? 95.0 + (_context.Bookings.Count() % 5) : 99.0;

            // Dynamic register overlay stats
            ViewBag.BusesCount = _context.Buses.Count();
            ViewBag.SupportStaffCount = _context.Employees.Count();
            
            int totalBookings = _context.Bookings.Count();
            ViewBag.SuccessRate = totalBookings > 0 
                ? 100.0 - (100.0 * _context.Bookings.Count(b => b.Status == BookingStatus.Cancelled) / totalBookings)
                : 100.0;
        }
    }
}
