using System.Collections.Generic;
using Second_Try.Models; // To access BookingRequest

namespace Second_Try.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalActiveBuses { get; set; }
        public int TotalActiveRoutes { get; set; }
        public int PendingBookingRequests { get; set; }
        public int TodayConfirmedBookings { get; set; }

        public IEnumerable<BookingRequest> RecentRequests { get; set; } = new List<BookingRequest>();
    }
}
