using Microsoft.EntityFrameworkCore;
using Second_Try.Data;
using Second_Try.Models;

namespace Second_Try.Services
{
    /// <summary>
    /// Background service that runs every hour.
    /// Marks any Pending booking request whose TravelDate is in the past as Expired (Cancelled),
    /// and sends a notification to the customer so they know automatically.
    /// </summary>
    public class RequestExpiryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RequestExpiryService> _logger;

        // Run every 1 hour
        private readonly TimeSpan _interval = TimeSpan.FromHours(1);

        public RequestExpiryService(
            IServiceScopeFactory scopeFactory,
            ILogger<RequestExpiryService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("✅ RequestExpiryService started.");

            // Run once immediately on startup, then on interval
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExpireOldRequestsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in RequestExpiryService");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task ExpireOldRequestsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateTime.UtcNow.Date;

            // Find all Pending requests where TravelDate is before today
            var expiredRequests = await db.BookingRequests
                .Where(r => r.Status == BookingRequestStatus.Pending
                         && r.TravelDate.Date < today)
                .Include(r => r.Route)
                .ToListAsync(ct);

            if (!expiredRequests.Any())
            {
                _logger.LogInformation("ℹ️  [ExpiryService] No expired requests found.");
                return;
            }

            var notifications = new List<Notification>();

            foreach (var req in expiredRequests)
            {
                // Mark as Cancelled with a system remark
                req.Status       = BookingRequestStatus.Cancelled;
                req.AdminRemarks = "Auto-expired: Travel date has passed without processing.";

                // Create a notification for the customer
                notifications.Add(new Notification
                {
                    CustomerId = req.CustomerId,
                    Title      = "Booking Request Expired",
                    Message    = $"Your booking request for " +
                                 $"{req.Route?.Origin ?? "Unknown"} → {req.Route?.Destination ?? "Unknown"} " +
                                 $"on {req.TravelDate:MMM dd, yyyy} has expired because the travel date passed " +
                                 $"before it was processed. Please submit a new request.",
                    IsRead    = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            db.Notifications.AddRange(notifications);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "✅ [ExpiryService] Expired {Count} old pending requests.",
                expiredRequests.Count);
        }
    }
}
