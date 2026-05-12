using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Second_Try.Data;
using Second_Try.Models;
using Second_Try.Services;
using System.Security.Claims;

namespace Second_Try.Controllers
{
    [Authorize(Roles = "Employee,Admin")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;

        public EmployeeController(ApplicationDbContext context, IEmailService email)
        {
            _context = context;
            _email   = email;
        }

        // ── Helper: get current employee ─────────────────────────
        private async Task<Employee?> GetCurrentEmployeeAsync()
        {
            string? email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return null;
            return await _context.Employees.FirstOrDefaultAsync(e => e.Email == email);
        }

        // ── E-01: Dashboard ───────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null) return RedirectToAction("Logout", "Auth");

            var now   = DateTime.UtcNow;
            var today = now.Date;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            // Stat counts
            ViewBag.TotalPending      = await _context.BookingRequests
                .CountAsync(r => r.Status == BookingRequestStatus.Pending);

            ViewBag.TodayPending      = await _context.BookingRequests
                .CountAsync(r => r.Status == BookingRequestStatus.Pending
                              && r.RequestDate.Date == today);

            ViewBag.AcceptedThisMonth = await _context.BookingRequests
                .CountAsync(r => r.Status == BookingRequestStatus.Accepted
                              && r.RequestDate >= monthStart);

            ViewBag.RejectedThisMonth = await _context.BookingRequests
                .CountAsync(r => r.Status == BookingRequestStatus.Rejected
                              && r.RequestDate >= monthStart);

            ViewBag.TotalBookings     = await _context.Bookings.CountAsync();

            // Latest 8 pending requests for feed
            ViewBag.RecentPending = await _context.BookingRequests
                .Include(r => r.Customer)
                .Include(r => r.Route)
                .Where(r => r.Status == BookingRequestStatus.Pending)
                .OrderByDescending(r => r.RequestDate)
                .Take(8)
                .ToListAsync();

            ViewBag.Employee = employee;
            return View();
        }

        // ── E-02: Booking Requests (GET) ──────────────────────────
        public async Task<IActionResult> BookingRequests(string? status, string? search)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null) return RedirectToAction("Logout", "Auth");

            var query = _context.BookingRequests
                .Include(r => r.Customer)
                .Include(r => r.Route)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingRequestStatus>(status, out var s))
                query = query.Where(r => r.Status == s);

            // Search by customer name or route
            if (!string.IsNullOrEmpty(search))
            {
                string q = search.ToLower();
                query = query.Where(r =>
                    (r.Customer != null && r.Customer.FullName.ToLower().Contains(q)) ||
                    (r.Route != null && (r.Route.Origin.ToLower().Contains(q) ||
                                         r.Route.Destination.ToLower().Contains(q))));
            }

            var requests = await query.OrderByDescending(r => r.RequestDate).ToListAsync();

            ViewBag.Requests    = requests;
            ViewBag.StatusFilter = status ?? "All";
            ViewBag.Search      = search ?? "";

            // Counts for filter tabs
            ViewBag.CountAll       = await _context.BookingRequests.CountAsync();
            ViewBag.CountPending   = await _context.BookingRequests.CountAsync(r => r.Status == BookingRequestStatus.Pending);
            ViewBag.CountAccepted  = await _context.BookingRequests.CountAsync(r => r.Status == BookingRequestStatus.Accepted);
            ViewBag.CountRejected  = await _context.BookingRequests.CountAsync(r => r.Status == BookingRequestStatus.Rejected);
            ViewBag.CountCancelled = await _context.BookingRequests.CountAsync(r => r.Status == BookingRequestStatus.Cancelled);

            ViewBag.Employee = employee;
            return View();
        }

        // ── E-03: Process Request (GET) ───────────────────────────
        public async Task<IActionResult> ProcessRequest(int id)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null) return RedirectToAction("Logout", "Auth");

            var request = await _context.BookingRequests
                .Include(r => r.Customer)
                .Include(r => r.Route)
                .Include(r => r.AssignedBooking)
                    .ThenInclude(b => b != null ? b.Bus : null)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            // Available buses of the preferred type
            ViewBag.AvailableBuses = await _context.Buses
                .Where(b => b.Type == request.PreferredBusType && b.IsActive)
                .ToListAsync();

            ViewBag.Employee = employee;
            return View(request);
        }

        // ── E-03: Accept Request (POST) ───────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(
            int requestId, int busId,
            string seatNumbers, decimal totalFare)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null) return RedirectToAction("Logout", "Auth");

            var request = await _context.BookingRequests
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null || request.Status != BookingRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "Request not found or already processed.";
                return RedirectToAction(nameof(BookingRequests));
            }

            // Create Booking record
            var booking = new Booking
            {
                BookingRequestId = request.Id,
                BusId            = busId,
                SeatNumbers      = seatNumbers.Trim(),
                TotalFare        = totalFare,
                Status           = BookingStatus.Confirmed,
                ConfirmedAt      = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);

            // Update request status
            request.Status = BookingRequestStatus.Accepted;

            // Write notification to customer
            _context.Notifications.Add(new Notification
            {
                CustomerId = request.CustomerId,
                Title      = "Booking Accepted",
                Message    = $"Your booking request REQ-{request.Id:D4} has been accepted! " +
                             $"Seats: {seatNumbers}. Total fare: PKR {totalFare:N0}.",
                IsRead     = false,
                CreatedAt  = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // 📧 Email customer (fire-and-forget)
            if (request.Customer != null)
            {
                var route    = await _context.Routes.FindAsync(request.RouteId);
                string routeStr = route != null ? $"{route.Origin} → {route.Destination}" : "N/A";
                _ = _email.SendBookingConfirmedEmailAsync(
                    request.Customer.Email, request.Customer.FullName,
                    routeStr, request.TravelDate,
                    request.NumberOfSeats, request.PreferredBusType.ToString());
            }

            TempData["SuccessMessage"] = $"Request REQ-{request.Id:D4} accepted and ticket confirmed!";
            return RedirectToAction(nameof(BookingRequests));
        }

        // ── E-03: Reject Request (POST) ───────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int requestId, string? remarks)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null) return RedirectToAction("Logout", "Auth");

            var request = await _context.BookingRequests
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null || request.Status != BookingRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "Request not found or already processed.";
                return RedirectToAction(nameof(BookingRequests));
            }

            request.Status        = BookingRequestStatus.Rejected;
            request.AdminRemarks  = remarks?.Trim();

            // Notify customer
            _context.Notifications.Add(new Notification
            {
                CustomerId = request.CustomerId,
                Title      = "Booking Rejected",
                Message    = $"Your booking request REQ-{request.Id:D4} was not accepted." +
                             (string.IsNullOrEmpty(remarks) ? "" : $" Reason: {remarks}"),
                IsRead     = false,
                CreatedAt  = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // 📧 Email customer (fire-and-forget)
            if (request.Customer != null)
            {
                var route    = await _context.Routes.FindAsync(request.RouteId);
                string routeStr = route != null ? $"{route.Origin} → {route.Destination}" : "N/A";
                _ = _email.SendBookingRejectedEmailAsync(
                    request.Customer.Email, request.Customer.FullName,
                    routeStr, request.TravelDate, remarks);
            }

            TempData["SuccessMessage"] = $"Request REQ-{request.Id:D4} has been rejected.";
            return RedirectToAction(nameof(BookingRequests));
        }

        // ── E-04: Booking History (GET) ───────────────────────────
        public async Task<IActionResult> BookingHistory(string? search)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null) return RedirectToAction("Logout", "Auth");

            var query = _context.Bookings
                .Include(b => b.BookingRequest)
                    .ThenInclude(r => r != null ? r.Customer : null)
                .Include(b => b.BookingRequest)
                    .ThenInclude(r => r != null ? r.Route : null)
                .Include(b => b.Bus)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                string q = search.ToLower();
                query = query.Where(b =>
                    (b.BookingRequest != null && b.BookingRequest.Customer != null &&
                     b.BookingRequest.Customer.FullName.ToLower().Contains(q)) ||
                    (b.Bus != null && b.Bus.BusNumber.ToLower().Contains(q)) ||
                    b.SeatNumbers.ToLower().Contains(q));
            }

            var bookings = await query.OrderByDescending(b => b.ConfirmedAt).ToListAsync();
            ViewBag.Bookings = bookings;
            ViewBag.Search   = search ?? "";
            ViewBag.Employee = employee;
            return View();
        }

        // ── E-05: Print Ticket (GET) ──────────────────────────────
        public async Task<IActionResult> PrintTicket(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingRequest)
                    .ThenInclude(r => r != null ? r.Customer : null)
                .Include(b => b.BookingRequest)
                    .ThenInclude(r => r != null ? r.Route : null)
                .Include(b => b.Bus)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // ── E-06: Employee Profile (GET) ──────────────────────────
        public async Task<IActionResult> EmployeeProfile()
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null) return RedirectToAction("Logout", "Auth");

            ViewBag.Employee = employee;
            return View(employee);
        }

        // ── E-06: Update Profile (POST) ──────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmployeeProfile(string FullName)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null) return RedirectToAction("Logout", "Auth");

            if (string.IsNullOrWhiteSpace(FullName))
            {
                TempData["ErrorMessage"] = "Full name cannot be empty.";
                return RedirectToAction(nameof(EmployeeProfile));
            }

            employee.FullName = FullName.Trim();
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction(nameof(EmployeeProfile));
        }

        // ── E-06: Change Password (POST) ─────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmployeePassword(
            string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null) return RedirectToAction("Logout", "Auth");

            if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, employee.PasswordHash))
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToAction(nameof(EmployeeProfile));
            }

            if (NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "New passwords do not match.";
                return RedirectToAction(nameof(EmployeeProfile));
            }

            var regex = new System.Text.RegularExpressions.Regex(
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@@#$%^&*]).{8,}$");
            if (!regex.IsMatch(NewPassword))
            {
                TempData["ErrorMessage"] = "Password must be 8+ chars with uppercase, lowercase, digit and special character.";
                return RedirectToAction(nameof(EmployeeProfile));
            }

            employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction(nameof(EmployeeProfile));
        }

        // ── E-06: Upload Profile Picture (POST) ──────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(IFormFile picture)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null) return RedirectToAction("Logout", "Auth");

            if (picture == null || picture.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an image file.";
                return RedirectToAction(nameof(EmployeeProfile));
            }

            string[] allowed = [".jpg", ".jpeg", ".png", ".webp"];
            string ext = Path.GetExtension(picture.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                TempData["ErrorMessage"] = "Only JPG, PNG, or WebP images are allowed.";
                return RedirectToAction(nameof(EmployeeProfile));
            }

            if (picture.Length > 3 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Image must be smaller than 3 MB.";
                return RedirectToAction(nameof(EmployeeProfile));
            }

            string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            Directory.CreateDirectory(uploadsDir);

            if (!string.IsNullOrEmpty(employee.ProfilePictureUrl))
            {
                string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    employee.ProfilePictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            string fileName = $"emp_{employee.Id}_{DateTime.UtcNow.Ticks}{ext}";
            string filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await picture.CopyToAsync(stream);

            employee.ProfilePictureUrl = $"/uploads/profiles/{fileName}";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile picture updated!";
            return RedirectToAction(nameof(EmployeeProfile));
        }

        // ── E-06: Upload Banner (POST) ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadBanner(IFormFile banner)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null) return RedirectToAction("Logout", "Auth");

            if (banner == null || banner.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an image file.";
                return RedirectToAction(nameof(EmployeeProfile));
            }

            string[] allowed = [".jpg", ".jpeg", ".png", ".webp"];
            string ext = Path.GetExtension(banner.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                TempData["ErrorMessage"] = "Only JPG, PNG, or WebP images are allowed.";
                return RedirectToAction(nameof(EmployeeProfile));
            }

            if (banner.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Banner must be smaller than 5 MB.";
                return RedirectToAction(nameof(EmployeeProfile));
            }

            string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "banners");
            Directory.CreateDirectory(uploadsDir);

            if (!string.IsNullOrEmpty(employee.CoverPictureUrl))
            {
                string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    employee.CoverPictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            string fileName = $"emp_banner_{employee.Id}_{DateTime.UtcNow.Ticks}{ext}";
            string filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await banner.CopyToAsync(stream);

            employee.CoverPictureUrl = $"/uploads/banners/{fileName}";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cover banner updated!";
            return RedirectToAction(nameof(EmployeeProfile));
        }
    }
}
