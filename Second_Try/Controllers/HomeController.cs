using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Second_Try.Data;
using Second_Try.Models;
using System.Diagnostics;

namespace Second_Try.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Second_Try.Services.IEmailService _emailService;

        public HomeController(ApplicationDbContext context, Second_Try.Services.IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch distinct locations for search dropdowns
            var origins = await _context.Routes.Select(r => r.Origin).Distinct().ToListAsync();
            var destinations = await _context.Routes.Select(r => r.Destination).Distinct().ToListAsync();
            
            ViewBag.Locations = origins.Concat(destinations)
                                       .Distinct()
                                       .OrderBy(l => l)
                                       .ToList();

            // Fetch top 3 latest 4+ star reviews
            var latestReviews = await _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.BookingRequest)
                    .ThenInclude(br => br!.Route)
                .Where(r => r.Rating >= 4)
                .OrderByDescending(r => r.CreatedAt)
                .Take(3)
                .ToListAsync();

            ViewBag.Reviews = latestReviews;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchBuses(string origin, string destination, DateTime travelDate)
        {
            if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination) || travelDate < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Please select valid origin, destination, and a future date.";
                return RedirectToAction(nameof(Index));
            }

            // Find routes matching the search
            var routes = await _context.Routes
                .Where(r => r.IsActive && r.Origin.ToLower() == origin.ToLower() && r.Destination.ToLower() == destination.ToLower())
                .Select(r => r.Id)
                .ToListAsync();

            if (!routes.Any())
            {
                ViewBag.NoResults = true;
                return View("SearchResults", new List<BusSchedule>());
            }

            // Get active schedules for these routes, including prices
            var schedules = await _context.BusSchedules
                .Include(s => s.Route)
                .Where(s => s.IsActive && routes.Contains(s.RouteId))
                .OrderBy(s => s.DepartureTime)
                .ToListAsync();

            // Fetch pricing map
            var priceLists = await _context.PriceLists
                .Where(p => routes.Contains(p.RouteId))
                .ToListAsync();

            ViewBag.Prices = priceLists;
            ViewBag.SearchOrigin = origin;
            ViewBag.SearchDestination = destination;
            ViewBag.SearchDate = travelDate;

            return View("SearchResults", schedules);
        }

        // ── 1. Popular Routes ──────────────────────────────────────
        public async Task<IActionResult> PopularRoutes()
        {
            // Fetch active routes, order by Origin
            var routes = await _context.Routes.Where(r => r.IsActive).OrderBy(r => r.Origin).ToListAsync();
            return View(routes);
        }

        // ── 2. Timetables ──────────────────────────────────────────
        public async Task<IActionResult> Timetables()
        {
            // Fetch active schedules and group by route
            var schedules = await _context.BusSchedules
                .Include(s => s.Route)
                .Where(s => s.IsActive && s.Route!.IsActive)
                .OrderBy(s => s.Route!.Origin)
                .ThenBy(s => s.DepartureTime)
                .ToListAsync();
            return View(schedules);
        }

        // ── 3. Static Policies & Info ──────────────────────────────
        public IActionResult CancellationPolicy() => View();
        public IActionResult FAQ() => View();
        public IActionResult Terms() => View();
        public IActionResult Privacy() => View();

        // ── 4. Contact Us ──────────────────────────────────────────
        public IActionResult Contact() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitContact(string name, string email, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "All required fields must be filled.";
                return RedirectToAction(nameof(Contact));
            }

            var contactMsg = new ContactMessage
            {
                Name = name.Trim(),
                Email = email.Trim(),
                Subject = string.IsNullOrWhiteSpace(subject) ? "General Inquiry" : subject.Trim(),
                Message = message.Trim()
            };
            
            _context.ContactMessages.Add(contactMsg);
            await _context.SaveChangesAsync();

            // Notify Admin via Email
            string adminBody = $@"
                <h2>New Message from SRCTravel Contact Form</h2>
                <p><strong>Name:</strong> {contactMsg.Name}</p>
                <p><strong>Email:</strong> {contactMsg.Email}</p>
                <p><strong>Subject:</strong> {contactMsg.Subject}</p>
                <hr/>
                <p>{contactMsg.Message}</p>
            ";
            _ = _emailService.SendEmailAsync("support@srctravel.pk", "Admin", $"New Contact Message: {contactMsg.Subject}", adminBody);

            TempData["SuccessMessage"] = "Your message has been sent successfully. We will get back to you shortly!";
            return RedirectToAction(nameof(Contact));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
