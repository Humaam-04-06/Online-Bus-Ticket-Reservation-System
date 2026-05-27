using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Second_Try.Data;
using Second_Try.Models;
using Second_Try.Services;

namespace Second_Try.Controllers
{
    [Authorize(Roles = "Customer")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;
        private readonly TicketPdfService _ticketPdf;

        public CustomerController(ApplicationDbContext context, IEmailService email, TicketPdfService ticketPdf)
        {
            _context   = context;
            _email     = email;
            _ticketPdf = ticketPdf;
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
                .Include(r => r.BusSchedule)
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
                            (r.Status == BookingRequestStatus.Accepted && r.TravelDate.Date >= DateTime.Today))
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

        // ── C-02b: Book Trip / Internal Search (GET) ──────────────────────
        [HttpGet]
        public async Task<IActionResult> BookTrip(string? origin, string? destination, DateTime? travelDate)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");
            SetCommonViewBag(customer);

            // Fetch distinct locations for search dropdowns
            var origins = await _context.Routes.Select(r => r.Origin).Distinct().ToListAsync();
            var destinations = await _context.Routes.Select(r => r.Destination).Distinct().ToListAsync();
            ViewBag.Locations = origins.Concat(destinations).Distinct().OrderBy(l => l).ToList();

            ViewBag.Origin = origin;
            ViewBag.Destination = destination;
            ViewBag.TravelDate = travelDate?.ToString("yyyy-MM-dd");

            if (travelDate.HasValue && travelDate.Value.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Please select a future date.";
            }

            // Perform dynamic search
            var routeQuery = _context.Routes.Where(r => r.IsActive).AsQueryable();

            if (!string.IsNullOrEmpty(origin))
            {
                routeQuery = routeQuery.Where(r => r.Origin.ToLower() == origin.ToLower());
            }

            if (!string.IsNullOrEmpty(destination))
            {
                routeQuery = routeQuery.Where(r => r.Destination.ToLower() == destination.ToLower());
            }

            var routeIds = await routeQuery.Select(r => r.Id).ToListAsync();

            var schedules = await _context.BusSchedules
                .Include(s => s.Route)
                .Where(s => routeIds.Contains(s.RouteId) && s.IsActive)
                .OrderBy(s => s.DepartureTime)
                .ToListAsync();

            var prices = await _context.PriceLists
                .Where(p => routeIds.Contains(p.RouteId))
                .ToListAsync();
            
            ViewBag.Prices = prices;

            if (!schedules.Any())
            {
                ViewBag.NoResults = true;
            }

            // Ensure travel date is set so the booking link doesn't break
            if (string.IsNullOrEmpty(ViewBag.TravelDate))
            {
                ViewBag.TravelDate = DateTime.Today.ToString("yyyy-MM-dd");
            }

            return View(schedules);
        }

        // ── C-03: New Request (GET) ───────────────────────────────────────
        public async Task<IActionResult> NewRequest(int? preselectRouteId = null, int? preselectScheduleId = null, string? preselectDate = null)
        {
            // If no schedule preselected, send to portal's internal search
            if (!preselectScheduleId.HasValue)
            {
                return RedirectToAction(nameof(BookTrip));
            }

            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");

            bool hasActive = await _context.BookingRequests.AnyAsync(r =>
                r.CustomerId == customer.Id &&
                (r.Status == BookingRequestStatus.Pending || 
                (r.Status == BookingRequestStatus.Accepted && r.TravelDate.Date >= DateTime.Today)));

            ViewBag.HasActiveRequest = hasActive;

            var sched = await _context.BusSchedules.Include(s => s.Route).FirstOrDefaultAsync(s => s.Id == preselectScheduleId);
            if (sched != null)
            {
                ViewBag.PreOrigin     = sched.Route!.Origin;
                ViewBag.PreDest       = sched.Route!.Destination;
                ViewBag.PreDate       = preselectDate;
                ViewBag.PreBusType    = sched.BusType.ToString();
                ViewBag.PreScheduleId = sched.Id;
                ViewBag.PreRouteId    = sched.RouteId;
                ViewBag.BusType       = sched.BusType; // for seat map rendering
            }

            SetCommonViewBag(customer);
            return View();
        }

        // ── C-03: New Request (POST) ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewRequest(
            string Origin, string Destination,
            DateTime TravelDate, int NumberOfSeats,
            BusType PreferredBusType, int? BusScheduleId, int? RouteId,
            string? SelectedSeatNumbers)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");

            // Block if already has an active request
            bool hasActive = await _context.BookingRequests.AnyAsync(r =>
                r.CustomerId == customer.Id &&
                (r.Status == BookingRequestStatus.Pending || 
                (r.Status == BookingRequestStatus.Accepted && r.TravelDate.Date >= DateTime.Today)));

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

            if (RouteId.HasValue)
            {
                var r = await _context.Routes.FindAsync(RouteId.Value);
                if (r != null)
                {
                    Origin = r.Origin;
                    Destination = r.Destination;
                }
            }
            else
            {
                var r = await _context.Routes.FirstOrDefaultAsync(ro => ro.Origin == Origin && ro.Destination == Destination && ro.IsActive);
                if (r == null)
                {
                    r = new Second_Try.Models.Route { Origin = Origin, Destination = Destination, EstimatedDurationHours = 0, IsActive = true };
                    _context.Routes.Add(r);
                    await _context.SaveChangesAsync();
                }
                RouteId = r.Id;
            }
            if (string.IsNullOrWhiteSpace(SelectedSeatNumbers))
            {
                ModelState.AddModelError("", "Please select at least one seat.");
                ViewBag.HasActiveRequest = false;
                return View();
            }

            // Backend Double-Booking Validation
            var requestedSeats = SelectedSeatNumbers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            
            var alreadyBooked = await _context.BookingRequests
                .Where(r => r.BusScheduleId == BusScheduleId
                         && r.TravelDate.Date == TravelDate.Date
                         && (r.Status == BookingRequestStatus.Pending ||
                             r.Status == BookingRequestStatus.Accepted ||
                             r.Status == BookingRequestStatus.Completed))
                .Select(r => r.SelectedSeatNumbers)
                .ToListAsync();

            var allTaken = alreadyBooked
                .SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(s => s.Trim())
                .ToHashSet();

            foreach (var seat in requestedSeats)
            {
                if (allTaken.Contains(seat))
                {
                    ModelState.AddModelError("", $"Seat {seat} is already booked or requested by another user. Please select different seats.");
                    ViewBag.HasActiveRequest = false;
                    return View();
                }
            }

            var request = new BookingRequest
            {
                CustomerId          = customer.Id,
                RouteId             = RouteId.Value,
                BusScheduleId       = BusScheduleId,
                PreferredBusType    = PreferredBusType,
                TravelDate          = TravelDate,
                NumberOfSeats       = NumberOfSeats,
                SelectedSeatNumbers = SelectedSeatNumbers ?? string.Empty,
                Status              = BookingRequestStatus.Pending,
                RequestDate         = DateTime.UtcNow
            };

            _context.BookingRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Booking request submitted successfully! Your request ID is REQ-{request.Id:D4}.";
            return RedirectToAction(nameof(MyRequests));
        }

        // ── C-03b: Get Booked Seats API (JSON) ────────────────────────────
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetBookedSeats(int scheduleId, DateTime travelDate)
        {
            var requests = await _context.BookingRequests
                .Include(r => r.AssignedBooking)
                .Where(r => r.BusScheduleId == scheduleId
                         && r.TravelDate.Date == travelDate.Date
                         && (r.Status == BookingRequestStatus.Pending ||
                             r.Status == BookingRequestStatus.Accepted ||
                             r.Status == BookingRequestStatus.Completed))
                .ToListAsync();

            var bookedSeats = new HashSet<string>();
            foreach (var r in requests)
            {
                if (!string.IsNullOrEmpty(r.SelectedSeatNumbers))
                {
                    var seats = r.SelectedSeatNumbers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var s in seats) bookedSeats.Add(s.Trim());
                }
                
                if (r.AssignedBooking != null && !string.IsNullOrEmpty(r.AssignedBooking.SeatNumbers))
                {
                    var seats = r.AssignedBooking.SeatNumbers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var s in seats) bookedSeats.Add(s.Trim());
                }
            }

            return Json(bookedSeats.ToList());
        }

        // ── C-04: My Requests ─────────────────────────────────────────────
        public async Task<IActionResult> MyRequests()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");

            var requests = await _context.BookingRequests
                .Where(r => r.CustomerId == customer.Id)
                .Include(r => r.Route)
                .Include(r => r.BusSchedule)
                .Include(r => r.AssignedBooking)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            ViewBag.Requests = requests;
            SetCommonViewBag(customer);
            return View();
        }

        // ── C-04b: Download PDF Ticket ────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DownloadTicket(int requestId)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");

            var req = await _context.BookingRequests
                .Include(r => r.Route)
                .Include(r => r.Customer)
                .Include(r => r.BusSchedule)
                .Include(r => r.AssignedBooking)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.CustomerId == customer.Id);

            if (req == null) return NotFound();

            // Only allow download if request is Accepted or Completed
            if (req.Status != BookingRequestStatus.Accepted && req.Status != BookingRequestStatus.Completed)
            {
                TempData["ErrorMessage"] = "Ticket is only available for accepted requests.";
                return RedirectToAction(nameof(MyRequests));
            }

            try
            {
                byte[] pdfBytes = _ticketPdf.GenerateTicket(req);
                string filename = $"SRCTravel-Ticket-TKT{req.Id:D6}.pdf";
                return File(pdfBytes, "application/pdf", filename);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Could not generate ticket: {ex.Message}";
                return RedirectToAction(nameof(MyRequests));
            }
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

            // 📧 Send cancellation email (fire-and-forget)
            var route = await _context.Routes.FindAsync(request.RouteId);
            string routeStr = route != null ? $"{route.Origin} → {route.Destination}" : "N/A";
            _ = _email.SendBookingCancelledEmailAsync(
                customer.Email, customer.FullName, routeStr, request.TravelDate);

            TempData["SuccessMessage"] = $"Request REQ-{requestId:D4} has been cancelled successfully.";
            return RedirectToAction(nameof(MyRequests));
        }

        // ── Download Ticket ─────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(Second_Try.Models.ViewModels.SubmitReviewViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid review data provided.";
                return RedirectToAction(nameof(MyRequests));
            }

            var customer = await GetCurrentCustomerAsync();
            if (customer == null) return RedirectToAction("Logout", "Auth");

            var request = await _context.BookingRequests
                .FirstOrDefaultAsync(r => r.Id == model.BookingRequestId && r.CustomerId == customer.Id);

            if (request == null || request.Status != BookingRequestStatus.Completed)
            {
                TempData["ErrorMessage"] = "You can only review completed trips.";
                return RedirectToAction(nameof(MyRequests));
            }

            // Check if review already exists
            bool hasReview = await _context.Reviews.AnyAsync(r => r.BookingRequestId == model.BookingRequestId);
            if (hasReview)
            {
                TempData["ErrorMessage"] = "You have already reviewed this trip.";
                return RedirectToAction(nameof(MyRequests));
            }

            var review = new Review
            {
                CustomerId       = customer.Id,
                BookingRequestId = model.BookingRequestId,
                Rating           = model.Rating,
                Comment          = model.Comment,
                CreatedAt        = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you for your review!";
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

            // Email format check
            var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            if (!emailAttr.IsValid(Email))
            {
                TempData["ErrorMessage"] = "Please enter a valid email address.";
                TempData["ActiveTab"] = "info";
                return RedirectToAction(nameof(Profile));
            }

            // Phone format check (digits and optionally +)
            if (!string.IsNullOrWhiteSpace(PhoneNumber))
            {
                var cleanedPhone = PhoneNumber.Trim();
                if (!System.Text.RegularExpressions.Regex.IsMatch(cleanedPhone, @"^\+?[0-9]+$"))
                {
                    TempData["ErrorMessage"] = "Phone number must contain only numbers (and optional '+' prefix).";
                    TempData["ActiveTab"] = "info";
                    return RedirectToAction(nameof(Profile));
                }
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
