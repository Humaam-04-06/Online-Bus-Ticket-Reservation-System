using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Second_Try.Data;
using Second_Try.Models;

namespace Second_Try.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── helper: get current customer from DB ──────────────────────────
        private async Task<Customer?> GetCurrentCustomerAsync()
        {
            string? email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return null;
            return await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
        }

        // ── helper: set common profile ViewBag fields ─────────────────────
        private void SetCommonViewBag(Customer customer,
            List<Notification>? notifications = null)
        {
            ViewBag.ProfilePicUrl  = customer.ProfilePictureUrl;
            ViewBag.CoverPicUrl    = customer.CoverPictureUrl;
            ViewBag.CustomerName   = customer.FullName;
            var notifList = notifications ?? new List<Notification>();
            ViewBag.Notifications  = notifList.Take(6).ToList();
            ViewBag.UnreadCount    = notifList.Count(n => !n.IsRead);
        }

        // ── C-02: Dashboard ───────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");

            var requests = await _context.BookingRequests
                .Where(r => r.CustomerId == customer.Id)
                .Include(r => r.Route)
                .ToListAsync();

            ViewBag.TotalRequests    = requests.Count;
            ViewBag.PendingCount     = requests.Count(r => r.Status == BookingRequestStatus.Pending);
            ViewBag.AcceptedCount    = requests.Count(r => r.Status == BookingRequestStatus.Accepted);
            ViewBag.RejectedCount    = requests.Count(r =>
                                           r.Status == BookingRequestStatus.Rejected ||
                                           r.Status == BookingRequestStatus.Cancelled);

            // Most recent active request
            ViewBag.ActiveRequest = requests
                .Where(r => r.Status == BookingRequestStatus.Pending ||
                            r.Status == BookingRequestStatus.Accepted)
                .OrderByDescending(r => r.RequestDate)
                .FirstOrDefault();

            // Real notifications from DB
            var notifications = await _context.Notifications
                .Where(n => n.CustomerId == customer.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(6)
                .ToListAsync();

            SetCommonViewBag(customer, notifications);
            return View();
        }

        // ── C-03: New Request (GET) ───────────────────────────────────────
        public async Task<IActionResult> NewRequest()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");

            bool hasActive = await _context.BookingRequests.AnyAsync(r =>
                r.CustomerId == customer.Id &&
                (r.Status == BookingRequestStatus.Pending || r.Status == BookingRequestStatus.Accepted));

            ViewBag.HasActiveRequest = hasActive;
            SetCommonViewBag(customer);
            return View();
        }

        // ── C-03: New Request (POST) ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewRequest(
            string Origin, string Destination,
            DateTime TravelDate, int NumberOfSeats,
            BusType PreferredBusType)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");

            // Block if already has an active request
            bool hasActive = await _context.BookingRequests.AnyAsync(r =>
                r.CustomerId == customer.Id &&
                (r.Status == BookingRequestStatus.Pending || r.Status == BookingRequestStatus.Accepted));

            if (hasActive)
            {
                TempData["ErrorMessage"] = "You already have an active request. Please wait for it to be resolved.";
                ViewBag.HasActiveRequest = true;
                return View();
            }

            if (string.IsNullOrWhiteSpace(Origin) || string.IsNullOrWhiteSpace(Destination) || Origin == Destination)
            {
                ModelState.AddModelError("", "Please select valid, different origin and destination cities.");
                ViewBag.HasActiveRequest = false;
                return View();
            }

            // Find existing route or create a new one
            var route = await _context.Routes.FirstOrDefaultAsync(r =>
                r.Origin == Origin && r.Destination == Destination && r.IsActive);

            if (route == null)
            {
                route = new Second_Try.Models.Route
                {
                    Origin = Origin,
                    Destination = Destination,
                    EstimatedDurationHours = 0,
                    IsActive = true
                };
                _context.Routes.Add(route);
                await _context.SaveChangesAsync();
            }

            var request = new BookingRequest
            {
                CustomerId      = customer.Id,
                RouteId         = route.Id,
                PreferredBusType = PreferredBusType,
                TravelDate      = TravelDate,
                NumberOfSeats   = NumberOfSeats,
                Status          = BookingRequestStatus.Pending,
                RequestDate     = DateTime.UtcNow
            };

            _context.BookingRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Booking request submitted successfully! Your request ID is REQ-{request.Id:D4}.";
            return RedirectToAction(nameof(MyRequests));
        }

        // ── C-04: My Requests ─────────────────────────────────────────────
        public async Task<IActionResult> MyRequests()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");

            var requests = await _context.BookingRequests
                .Where(r => r.CustomerId == customer.Id)
                .Include(r => r.Route)
                .Include(r => r.AssignedBooking)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            ViewBag.Requests = requests;
            SetCommonViewBag(customer);
            return View();
        }

        // ── Cancel a pending request ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int requestId)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");

            var request = await _context.BookingRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.CustomerId == customer.Id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction(nameof(MyRequests));
            }

            if (request.Status != BookingRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "Only pending requests can be cancelled.";
                return RedirectToAction(nameof(MyRequests));
            }

            request.Status = BookingRequestStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Request REQ-{requestId:D4} has been cancelled successfully.";
            return RedirectToAction(nameof(MyRequests));
        }

        // ── Profile (GET) ─────────────────────────────────────────
        public async Task<IActionResult> Profile()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");

            var requests = await _context.BookingRequests
                .Where(r => r.CustomerId == customer.Id)
                .ToListAsync();

            ViewBag.TotalRequests  = requests.Count;
            ViewBag.AcceptedCount  = requests.Count(r => r.Status == BookingRequestStatus.Accepted);
            ViewBag.PendingCount   = requests.Count(r => r.Status == BookingRequestStatus.Pending);
            SetCommonViewBag(customer);

            return View(customer);
        }

        // ── UpdateProfile (POST) ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            string FullName, string Email, string? PhoneNumber, string activeTab)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");

            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email))
            {
                TempData["ErrorMessage"] = "Full name and email are required.";
                TempData["ActiveTab"] = "info";
                return RedirectToAction(nameof(Profile));
            }

            // Check email uniqueness (another account using same email)
            bool emailTaken = await _context.Customers.AnyAsync(c =>
                c.Email == Email.Trim() && c.Id != customer.Id);

            if (emailTaken)
            {
                TempData["ErrorMessage"] = "That email address is already used by another account.";
                TempData["ActiveTab"] = "info";
                return RedirectToAction(nameof(Profile));
            }

            customer.FullName    = FullName.Trim();
            customer.Email       = Email.Trim().ToLower();
            customer.PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim();

            await _context.SaveChangesAsync();

            // Refresh the auth cookie name claim
            var identity = (System.Security.Claims.ClaimsIdentity)User.Identity!;
            var nameClaim = identity.FindFirst(System.Security.Claims.ClaimTypes.Name);
            if (nameClaim != null) identity.RemoveClaim(nameClaim);
            identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, customer.FullName));

            TempData["SuccessMessage"] = "Profile updated successfully!";
            TempData["ActiveTab"] = "info";
            return RedirectToAction(nameof(Profile));
        }

        // ── UpdatePassword (POST) ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(
            string CurrentPassword, string NewPassword, string ConfirmPassword, string activeTab)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");

            TempData["ActiveTab"] = "security";

            if (string.IsNullOrEmpty(customer.PasswordHash))
            {
                TempData["ErrorMessage"] = "Password change is not available for Google-login accounts.";
                return RedirectToAction(nameof(Profile));
            }

            if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, customer.PasswordHash))
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToAction(nameof(Profile));
            }

            if (NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "New passwords do not match.";
                return RedirectToAction(nameof(Profile));
            }

            var regex = new System.Text.RegularExpressions.Regex(
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{}|;':"",./<>?]).{8,}$");
            if (!regex.IsMatch(NewPassword))
            {
                TempData["ErrorMessage"] = "New password must be 8+ characters with uppercase, lowercase, digit, and special character.";
                return RedirectToAction(nameof(Profile));
            }

            customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction(nameof(Profile));
        }

        // ── UploadPicture (POST) ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(IFormFile picture)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");

            TempData["ActiveTab"] = "info";

            if (picture == null || picture.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an image file.";
                return RedirectToAction(nameof(Profile));
            }

            string[] allowed = [".jpg", ".jpeg", ".png", ".webp"];
            string ext = Path.GetExtension(picture.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                TempData["ErrorMessage"] = "Only JPG, PNG, or WebP images are allowed.";
                return RedirectToAction(nameof(Profile));
            }

            if (picture.Length > 3 * 1024 * 1024) // 3 MB
            {
                TempData["ErrorMessage"] = "Image must be smaller than 3 MB.";
                return RedirectToAction(nameof(Profile));
            }

            // Save to wwwroot/uploads/profiles/
            string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            Directory.CreateDirectory(uploadsDir);

            // Delete old picture if exists
            if (!string.IsNullOrEmpty(customer.ProfilePictureUrl))
            {
                string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    customer.ProfilePictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            string fileName = $"customer_{customer.Id}_{DateTime.UtcNow.Ticks}{ext}";
            string filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await picture.CopyToAsync(stream);

            customer.ProfilePictureUrl = $"/uploads/profiles/{fileName}";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile picture updated!";
            return RedirectToAction(nameof(Profile));
        }

        // ── Legacy Login redirect ─────────────────────────────────────────
        public IActionResult Login()
        {
            return RedirectToAction("Login", "Auth");
        }

        // ── UploadBanner (POST) ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadBanner(IFormFile banner)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");

            TempData["ActiveTab"] = "info";

            if (banner == null || banner.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an image file.";
                return RedirectToAction(nameof(Profile));
            }

            string[] allowed = [".jpg", ".jpeg", ".png", ".webp"];
            string ext = Path.GetExtension(banner.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                TempData["ErrorMessage"] = "Only JPG, PNG, or WebP images are allowed.";
                return RedirectToAction(nameof(Profile));
            }

            if (banner.Length > 5 * 1024 * 1024) // 5 MB
            {
                TempData["ErrorMessage"] = "Banner image must be smaller than 5 MB.";
                return RedirectToAction(nameof(Profile));
            }

            string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "banners");
            Directory.CreateDirectory(uploadsDir);

            // Delete old banner
            if (!string.IsNullOrEmpty(customer.CoverPictureUrl))
            {
                string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    customer.CoverPictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            string fileName = $"banner_{customer.Id}_{DateTime.UtcNow.Ticks}{ext}";
            string filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await banner.CopyToAsync(stream);

            customer.CoverPictureUrl = $"/uploads/banners/{fileName}";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cover banner updated!";
            return RedirectToAction(nameof(Profile));
        }

        // ── MarkNotificationsRead (POST) ──────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationsRead()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return Json(new { success = false });

            var unread = await _context.Notifications
                .Where(n => n.CustomerId == customer.Id && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread) n.IsRead = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
