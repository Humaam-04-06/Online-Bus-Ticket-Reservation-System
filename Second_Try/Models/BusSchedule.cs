using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Second_Try.Models
{
    public class BusSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RouteId { get; set; }

        [Required]
        public BusType BusType { get; set; }

        [Required]
        public TimeSpan DepartureTime { get; set; }

        [Required]
        public TimeSpan ArrivalTime { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        [ForeignKey(nameof(RouteId))]
        public Route? Route { get; set; }

        public ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();
    }
}
