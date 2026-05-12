using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Second_Try.Data;
using Second_Try.Models;

namespace Second_Try.Controllers
{
    /// <summary>
    /// Lightweight API controller — returns real PriceList fares from the DB.
    /// Called by the NewRequest page JavaScript to show live fare estimates.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FareController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public FareController(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// GET /api/fare?origin=Karachi&destination=Lahore
        /// Returns all 4 bus type fares for the given route, or 0 if not found.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetFares(string origin, string destination)
        {
            if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
                return BadRequest(new { error = "Origin and destination are required." });

            // Find matching active route
            var route = await _context.Routes
                .FirstOrDefaultAsync(r =>
                    r.Origin == origin &&
                    r.Destination == destination &&
                    r.IsActive);

            if (route == null)
            {
                // Route not in DB yet — return zeros so JS falls back to estimate
                return Ok(new
                {
                    found    = false,
                    economy  = 0,
                    standard = 0,
                    luxury   = 0,
                    express  = 0,
                    duration = 0
                });
            }

            // Fetch all price entries for this route
            var prices = await _context.PriceLists
                .Where(p => p.RouteId == route.Id)
                .ToListAsync();

            decimal Get(BusType t) =>
                prices.FirstOrDefault(p => p.BusType == t)?.FareAmount ?? 0;

            return Ok(new
            {
                found    = prices.Any(),
                routeId  = route.Id,
                duration = route.EstimatedDurationHours,
                economy  = Get(BusType.Economy),
                standard = Get(BusType.Standard),
                luxury   = Get(BusType.Luxury),
                express  = Get(BusType.Express)
            });
        }
    }
}
