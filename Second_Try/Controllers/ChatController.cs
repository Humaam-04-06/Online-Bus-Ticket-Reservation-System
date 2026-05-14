using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using Second_Try.Data;
using Second_Try.Services;
using Microsoft.EntityFrameworkCore;

namespace Second_Try.Controllers
{
    [Route("api/chat")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IGeminiService _gemini;
        private readonly ApplicationDbContext _context;

        public ChatController(IGeminiService gemini, ApplicationDbContext context)
        {
            _gemini  = gemini;
            _context = context;
        }

        // POST /api/chat/ask
        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Message))
                return BadRequest(new { error = "Message cannot be empty." });

            // ── Load session history ──────────────────────────────
            var history = LoadHistory();

            // ── Build personal context if logged in ───────────────
            string? userContext = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                string? email = User.FindFirstValue(ClaimTypes.Email);
                string? role  = User.FindFirstValue(ClaimTypes.Role);
                userContext   = await BuildUserContextAsync(email, role);
            }

            // ── Call Gemini ───────────────────────────────────────
            string answer = await _gemini.AskAsync(history, req.Message, userContext);

            // ── Update history in session ─────────────────────────
            history.Add(new ChatMessage { Role = "user",  Text = req.Message });
            history.Add(new ChatMessage { Role = "model", Text = answer });

            // Keep only last 30 messages in session
            if (history.Count > 30)
                history = history.TakeLast(30).ToList();

            SaveHistory(history);

            return Ok(new { reply = answer });
        }

        // DELETE /api/chat/clear — clears session history
        [HttpDelete("clear")]
        public IActionResult ClearHistory()
        {
            HttpContext.Session.Remove("chat_history");
            return Ok(new { cleared = true });
        }

        // ── Helpers ───────────────────────────────────────────────
        private List<ChatMessage> LoadHistory()
        {
            string? json = HttpContext.Session.GetString("chat_history");
            if (string.IsNullOrEmpty(json)) return new List<ChatMessage>();
            return JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new();
        }

        private void SaveHistory(List<ChatMessage> history)
        {
            HttpContext.Session.SetString("chat_history",
                JsonSerializer.Serialize(history));
        }

        private async Task<string?> BuildUserContextAsync(string? email, string? role)
        {
            if (string.IsNullOrEmpty(email)) return null;

            if (role == "Customer")
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == email);
                if (customer == null) return null;

                var requests = await _context.BookingRequests
                    .Where(r => r.CustomerId == customer.Id)
                    .Include(r => r.Route)
                    .OrderByDescending(r => r.RequestDate)
                    .Take(5)
                    .ToListAsync();

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Logged-in customer: {customer.FullName} ({customer.Email})");
                sb.AppendLine($"Total booking requests: {requests.Count}");
                if (requests.Any())
                {
                    sb.AppendLine("Recent requests:");
                    foreach (var r in requests)
                        sb.AppendLine($"  - REQ-{r.Id:D4}: {r.Route?.Origin} → {r.Route?.Destination}, " +
                                      $"Date: {r.TravelDate:MMM dd yyyy}, Status: {r.Status}, Seats: {r.NumberOfSeats}");
                }
                return sb.ToString();
            }

            if (role == "Admin" || role == "Employee")
            {
                var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Email == email);
                return emp == null ? null :
                    $"Logged-in {role}: {emp.FullName} ({emp.Email}), Role: {emp.Role}";
            }

            return null;
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
    }
}
