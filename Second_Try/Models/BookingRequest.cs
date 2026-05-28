using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Second_Try.Models
{
    public enum BookingRequestStatus
    {
        Pending,
        Accepted,
        Rejected,
        Cancelled,
        Completed
    }

    public class BookingRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int RouteId { get; set; }

        public int? BusScheduleId { get; set; }

        [Required]
        public BusType PreferredBusType { get; set; }

        [Required]
        public DateTime TravelDate { get; set; }

        [Required]
        [Range(1, 50, ErrorMessage = "You can request between 1 and 50 seats.")]
        public int NumberOfSeats { get; set; }

        [Required]
        [MaxLength(200)]
        public string SelectedSeatNumbers { get; set; } = string.Empty;

        [Required]
        public BookingRequestStatus Status { get; set; } = BookingRequestStatus.Pending;

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public string? AdminRemarks { get; set; } // e.g., Reason if rejected

        public int? AppliedVoucherId { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [ForeignKey(nameof(RouteId))]
        public Route? Route { get; set; }

        [ForeignKey(nameof(BusScheduleId))]
        public BusSchedule? BusSchedule { get; set; }

        [ForeignKey(nameof(AppliedVoucherId))]
        public Voucher? AppliedVoucher { get; set; }

        public Booking? AssignedBooking { get; set; } // Will be populated if accepted
    }
}
