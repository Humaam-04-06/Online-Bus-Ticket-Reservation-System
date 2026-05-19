using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Second_Try.Data;
using Second_Try.Models;
using System.Security.Claims;

namespace Second_Try.Controllers
{
    [Authorize(Roles = "Admin")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminController(ApplicationDbContext context) => _context = context;

        private async Task<Employee?> GetCurrentAdminAsync()
        {
            string? email = User.FindFirstValue(ClaimTypes.Email);
            return await _context.Employees.FirstOrDefaultAsync(e => e.Email == email);
        }

        // ── A-01: Dashboard ───────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            var now        = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            ViewBag.TotalCustomers   = await _context.Customers.CountAsync();
            ViewBag.TotalEmployees   = await _context.Employees.CountAsync();
            ViewBag.TotalBuses       = await _context.Buses.CountAsync(b => b.IsActive);
            ViewBag.TotalRoutes      = await _context.Routes.CountAsync(r => r.IsActive);
            ViewBag.TotalRequests    = await _context.BookingRequests.CountAsync();
            ViewBag.PendingRequests  = await _context.BookingRequests.CountAsync(r => r.Status == BookingRequestStatus.Pending);
            ViewBag.TotalBookings    = await _context.Bookings.CountAsync();
            ViewBag.RevenueThisMonth = await _context.Bookings
                .Where(b => b.ConfirmedAt >= monthStart)
                .SumAsync(b => (decimal?)b.TotalFare) ?? 0;

            ViewBag.RecentRequests = await _context.BookingRequests
                .Include(r => r.Customer).Include(r => r.Route)
                .OrderByDescending(r => r.RequestDate).Take(6).ToListAsync();

            ViewBag.Admin = await GetCurrentAdminAsync();
            return View();
        }

        // ══════════════════════════════════════════════════════════
        // MANAGE EMPLOYEES
        // ══════════════════════════════════════════════════════════
        public async Task<IActionResult> ManageEmployees(string? search)
        {
            var query = _context.Employees.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(e => e.FullName.Contains(search) || e.Email.Contains(search));

            ViewBag.Employees = await query.OrderBy(e => e.FullName).ToListAsync();
            ViewBag.Search    = search ?? "";
            return View();
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(string FullName, string Email, string Password, EmployeeRole Role)
        {
            if (await _context.Employees.AnyAsync(e => e.Email == Email))
            {
                TempData["ErrorMessage"] = "An employee with this email already exists.";
                return RedirectToAction(nameof(ManageEmployees));
            }
            _context.Employees.Add(new Employee
            {
                FullName     = FullName.Trim(),
                Email        = Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                Role         = Role,
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Employee '{FullName}' created successfully!";
            return RedirectToAction(nameof(ManageEmployees));
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEmployee(int id)
        {
            var emp = await _context.Employees.FindAsync(id);
            if (emp == null) return NotFound();

            // Prevent deactivating the last active Admin
            if (emp.IsActive && emp.Role == EmployeeRole.Admin)
            {
                var activeAdminsCount = await _context.Employees.CountAsync(e => e.Role == EmployeeRole.Admin && e.IsActive);
                if (activeAdminsCount <= 1)
                {
                    TempData["ErrorMessage"] = "Cannot deactivate the last active administrator.";
                    return RedirectToAction(nameof(ManageEmployees));
                }
            }

            emp.IsActive = !emp.IsActive;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Employee '{emp.FullName}' {(emp.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(ManageEmployees));
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var emp = await _context.Employees.FindAsync(id);
            if (emp == null) return NotFound();
            _context.Employees.Remove(emp);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Employee deleted.";
            return RedirectToAction(nameof(ManageEmployees));
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoute(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route != null)
            {
                bool hasSchedules = await _context.BusSchedules.AnyAsync(s => s.RouteId == id);
                bool hasBookings = await _context.BookingRequests.AnyAsync(b => b.RouteId == id);
                bool hasPrices = await _context.PriceLists.AnyAsync(p => p.RouteId == id);

                if (hasSchedules || hasBookings || hasPrices)
                {
                    TempData["ErrorMessage"] = "Cannot delete this route because it has active schedules, prices, or bookings. Please deactivate it instead.";
                }
                else
                {
                    _context.Routes.Remove(route);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Route deleted successfully.";
                }
            }
            return RedirectToAction(nameof(ManageRoutes));
        }

        // ══════════════════════════════════════════════════════════
        // MANAGE BUSES
        // ══════════════════════════════════════════════════════════
        public async Task<IActionResult> ManageBuses()
        {
            ViewBag.Buses = await _context.Buses.OrderBy(b => b.BusNumber).ToListAsync();
            return View();
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBus(string BusNumber, BusType Type, int Capacity, string? Amenities)
        {
            var normalizedNumber = BusNumber.Trim().ToUpper();
            if (await _context.Buses.AnyAsync(b => b.BusNumber == normalizedNumber))
            {
                TempData["ErrorMessage"] = "A bus with this number already exists.";
                return RedirectToAction(nameof(ManageBuses));
            }
            _context.Buses.Add(new Bus { BusNumber = normalizedNumber, Type = Type, Capacity = Capacity, Amenities = Amenities?.Trim(), IsActive = true });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Bus {normalizedNumber} added.";
            return RedirectToAction(nameof(ManageBuses));
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBus(int id, string BusNumber, BusType Type, int Capacity, string? Amenities)
        {
            var bus = await _context.Buses.FindAsync(id);
            if (bus == null) return NotFound();

            var normalizedNumber = BusNumber.Trim().ToUpper();
            if (await _context.Buses.AnyAsync(b => b.BusNumber == normalizedNumber && b.Id != id))
            {
                TempData["ErrorMessage"] = $"A bus with the number {normalizedNumber} already exists.";
                return RedirectToAction(nameof(ManageBuses));
            }

            bus.BusNumber  = normalizedNumber;
            bus.Type       = Type;
            bus.Capacity   = Capacity;
            bus.Amenities  = Amenities?.Trim();
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Bus updated.";
            return RedirectToAction(nameof(ManageBuses));
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBus(int id)
        {
            var bus = await _context.Buses.FindAsync(id);
            if (bus == null) return NotFound();
            bus.IsActive = !bus.IsActive;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Bus {bus.BusNumber} {(bus.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(ManageBuses));
        }

        // ══════════════════════════════════════════════════════════
        // MANAGE ROUTES
        // ══════════════════════════════════════════════════════════
        public async Task<IActionResult> ManageRoutes()
        {
            ViewBag.Routes = await _context.Routes
                .Include(r => r.PriceLists).OrderBy(r => r.Origin).ToListAsync();
            return View();
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoute(string Origin, string Destination, double EstimatedDurationHours)
        {
            bool exists = await _context.Routes.AnyAsync(r =>
                r.Origin == Origin && r.Destination == Destination);
            if (exists) { TempData["ErrorMessage"] = "This route already exists."; return RedirectToAction(nameof(ManageRoutes)); }
            _context.Routes.Add(new Models.Route { Origin = Origin.Trim(), Destination = Destination.Trim(), EstimatedDurationHours = EstimatedDurationHours, IsActive = true });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Route {Origin} → {Destination} added.";
            return RedirectToAction(nameof(ManageRoutes));
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRoute(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route == null) return NotFound();
            route.IsActive = !route.IsActive;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Route {(route.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(ManageRoutes));
        }

        // ══════════════════════════════════════════════════════════
        // MANAGE SCHEDULES
        // ══════════════════════════════════════════════════════════
        public async Task<IActionResult> ManageSchedules()
        {
            ViewBag.Schedules = await _context.BusSchedules
                .Include(s => s.Route)
                .OrderBy(s => s.Route!.Origin).ThenBy(s => s.DepartureTime)
                .ToListAsync();
            ViewBag.Routes = await _context.Routes.Where(r => r.IsActive).ToListAsync();
            return View();
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> UpsertSchedule(int id, int RouteId, BusType BusType, TimeSpan DepartureTime, TimeSpan ArrivalTime)
        {
            if (id == 0)
            {
                _context.BusSchedules.Add(new BusSchedule
                {
                    RouteId = RouteId,
                    BusType = BusType,
                    DepartureTime = DepartureTime,
                    ArrivalTime = ArrivalTime,
                    IsActive = true
                });
                TempData["SuccessMessage"] = "Bus schedule added successfully.";
            }
            else
            {
                var sched = await _context.BusSchedules.FindAsync(id);
                if (sched != null)
                {
                    sched.RouteId = RouteId;
                    sched.BusType = BusType;
                    sched.DepartureTime = DepartureTime;
                    sched.ArrivalTime = ArrivalTime;
                    TempData["SuccessMessage"] = "Bus schedule updated successfully.";
                }
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageSchedules));
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSchedule(int id)
        {
            var sched = await _context.BusSchedules.FindAsync(id);
            if (sched != null)
            {
                sched.IsActive = !sched.IsActive;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Schedule {(sched.IsActive ? "activated" : "deactivated")}.";
            }
            return RedirectToAction(nameof(ManageSchedules));
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var sched = await _context.BusSchedules.FindAsync(id);
            if (sched != null)
            {
                bool hasBookings = await _context.BookingRequests.AnyAsync(b => b.BusScheduleId == id);
                if (hasBookings)
                {
                    TempData["ErrorMessage"] = "Cannot delete this schedule because there are booking requests associated with it. Please deactivate it instead.";
                }
                else
                {
                    _context.BusSchedules.Remove(sched);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Schedule deleted successfully.";
                }
            }
            return RedirectToAction(nameof(ManageSchedules));
        }

        // ══════════════════════════════════════════════════════════
        // MANAGE PRICE LIST
        // ══════════════════════════════════════════════════════════
        public async Task<IActionResult> ManagePriceList()
        {
            ViewBag.PriceLists = await _context.PriceLists
                .Include(p => p.Route).OrderBy(p => p.Route!.Origin).ToListAsync();
            ViewBag.Routes = await _context.Routes.Where(r => r.IsActive).ToListAsync();
            return View();
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> UpsertPrice(int RouteId, BusType BusType, decimal FareAmount)
        {
            var existing = await _context.PriceLists
                .FirstOrDefaultAsync(p => p.RouteId == RouteId && p.BusType == BusType);
            if (existing != null)
                existing.FareAmount = FareAmount;
            else
                _context.PriceLists.Add(new PriceList { RouteId = RouteId, BusType = BusType, FareAmount = FareAmount });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Price saved.";
            return RedirectToAction(nameof(ManagePriceList));
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePrice(int id)
        {
            var p = await _context.PriceLists.FindAsync(id);
            if (p != null) { _context.PriceLists.Remove(p); await _context.SaveChangesAsync(); }
            TempData["SuccessMessage"] = "Price entry deleted.";
            return RedirectToAction(nameof(ManagePriceList));
        }

        // ══════════════════════════════════════════════════════════
        // BOOKING HISTORY
        // ══════════════════════════════════════════════════════════
        public async Task<IActionResult> BookingHistory(string? search)
        {
            var query = _context.Bookings
                .Include(b => b.BookingRequest).ThenInclude(r => r!.Customer)
                .Include(b => b.BookingRequest).ThenInclude(r => r!.Route)
                .Include(b => b.Bus).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                string q = search.ToLower();
                query = query.Where(b =>
                    (b.BookingRequest!.Customer!.FullName.ToLower().Contains(q)) ||
                    (b.Bus!.BusNumber.ToLower().Contains(q)));
            }
            ViewBag.Bookings = await query.OrderByDescending(b => b.ConfirmedAt).ToListAsync();
            ViewBag.Search   = search ?? "";
            return View();
        }

        // ══════════════════════════════════════════════════════════
        // REPORTS
        // ══════════════════════════════════════════════════════════
        public async Task<IActionResult> Reports()
        {
            var now        = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            ViewBag.TotalRevenue      = await _context.Bookings.SumAsync(b => (decimal?)b.TotalFare) ?? 0;
            ViewBag.RevenueThisMonth  = await _context.Bookings.Where(b => b.ConfirmedAt >= monthStart).SumAsync(b => (decimal?)b.TotalFare) ?? 0;
            ViewBag.TotalBookings     = await _context.Bookings.CountAsync();
            ViewBag.TotalCustomers    = await _context.Customers.CountAsync();
            ViewBag.PendingCount      = await _context.BookingRequests.CountAsync(r => r.Status == BookingRequestStatus.Pending);
            ViewBag.AcceptedCount     = await _context.BookingRequests.CountAsync(r => r.Status == BookingRequestStatus.Accepted);
            ViewBag.RejectedCount     = await _context.BookingRequests.CountAsync(r => r.Status == BookingRequestStatus.Rejected);
            ViewBag.TopRoutes         = await _context.Bookings
                .Include(b => b.BookingRequest).ThenInclude(r => r!.Route)
                .GroupBy(b => b.BookingRequest!.Route!.Origin + " → " + b.BookingRequest.Route.Destination)
                .Select(g => new { Route = g.Key, Count = g.Count(), Revenue = g.Sum(x => x.TotalFare) })
                .OrderByDescending(x => x.Count).Take(5).ToListAsync();
            return View();
        }

        // ══════════════════════════════════════════════════════════
        // ADMIN PROFILE
        // ══════════════════════════════════════════════════════════
        public async Task<IActionResult> AdminProfile()
        {
            var admin = await GetCurrentAdminAsync();
            if (admin == null) return RedirectToAction("Logout", "Auth");
            return View(admin);
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAdminProfile(string FullName)
        {
            var admin = await GetCurrentAdminAsync();
            if (admin == null) return RedirectToAction("Logout", "Auth");
            admin.FullName = FullName.Trim();
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile updated.";
            return RedirectToAction(nameof(AdminProfile));
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeAdminPassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            var admin = await GetCurrentAdminAsync();
            if (admin == null) return RedirectToAction("Logout", "Auth");
            if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, admin.PasswordHash))
            { TempData["ErrorMessage"] = "Current password is incorrect."; return RedirectToAction(nameof(AdminProfile)); }
            if (NewPassword != ConfirmPassword)
            { TempData["ErrorMessage"] = "New passwords do not match."; return RedirectToAction(nameof(AdminProfile)); }
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction(nameof(AdminProfile));
        }

        // ── A-07: Upload Profile Picture (POST) ──────────────────
        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(IFormFile picture)
        {
            var admin = await GetCurrentAdminAsync();
            if (admin == null) return RedirectToAction("Logout", "Auth");

            if (picture == null || picture.Length == 0)
            { TempData["ErrorMessage"] = "Please select an image file."; return RedirectToAction(nameof(AdminProfile)); }

            string[] allowed = [".jpg", ".jpeg", ".png", ".webp"];
            string ext = Path.GetExtension(picture.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            { TempData["ErrorMessage"] = "Only JPG, PNG, or WebP allowed."; return RedirectToAction(nameof(AdminProfile)); }

            if (picture.Length > 3 * 1024 * 1024)
            { TempData["ErrorMessage"] = "Image must be smaller than 3 MB."; return RedirectToAction(nameof(AdminProfile)); }

            string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            Directory.CreateDirectory(uploadsDir);

            if (!string.IsNullOrEmpty(admin.ProfilePictureUrl))
            {
                string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    admin.ProfilePictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            string fileName = $"adm_{admin.Id}_{DateTime.UtcNow.Ticks}{ext}";
            string filePath = Path.Combine(uploadsDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await picture.CopyToAsync(stream);

            admin.ProfilePictureUrl = $"/uploads/profiles/{fileName}";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile picture updated!";
            return RedirectToAction(nameof(AdminProfile));
        }

        // ── A-07: Upload Banner (POST) ────────────────────────────
        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadBanner(IFormFile banner)
        {
            var admin = await GetCurrentAdminAsync();
            if (admin == null) return RedirectToAction("Logout", "Auth");

            if (banner == null || banner.Length == 0)
            { TempData["ErrorMessage"] = "Please select an image file."; return RedirectToAction(nameof(AdminProfile)); }

            string[] allowed = [".jpg", ".jpeg", ".png", ".webp"];
            string ext = Path.GetExtension(banner.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            { TempData["ErrorMessage"] = "Only JPG, PNG, or WebP allowed."; return RedirectToAction(nameof(AdminProfile)); }

            if (banner.Length > 5 * 1024 * 1024)
            { TempData["ErrorMessage"] = "Banner must be smaller than 5 MB."; return RedirectToAction(nameof(AdminProfile)); }

            string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "banners");
            Directory.CreateDirectory(uploadsDir);

            if (!string.IsNullOrEmpty(admin.CoverPictureUrl))
            {
                string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    admin.CoverPictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            string fileName = $"adm_banner_{admin.Id}_{DateTime.UtcNow.Ticks}{ext}";
            string filePath = Path.Combine(uploadsDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await banner.CopyToAsync(stream);

            admin.CoverPictureUrl = $"/uploads/banners/{fileName}";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cover banner updated!";
            return RedirectToAction(nameof(AdminProfile));
        }
    }
}
