using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Second_Try.Models
{
    public enum BookingStatus
    {
        Confirmed,
        Completed,
        Cancelled
    }

    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingRequestId { get; set; }

        [Required]
        public int BusId { get; set; }

        [Required]
        public string SeatNumbers { get; set; } = string.Empty; // e.g., "12A, 12B"

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalFare { get; set; }

        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

        public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(BookingRequestId))]
        public BookingRequest? BookingRequest { get; set; }

        [ForeignKey(nameof(BusId))]
        public Bus? Bus { get; set; }
    }
}
