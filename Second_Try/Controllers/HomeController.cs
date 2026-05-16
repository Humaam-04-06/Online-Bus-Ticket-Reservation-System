using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Second_Try.Data;
using Second_Try.Models;
using System.Diagnostics;

namespace Second_Try.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
