using System.ComponentModel.DataAnnotations;

namespace Second_Try.Models
{
    public enum BusType
    {
        Economy,
        Standard,
        Luxury,
        Express
    }

    public class Bus
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string BusNumber { get; set; } = string.Empty; // e.g., AB-1234

        [Required]
        public BusType Type { get; set; } = BusType.Standard;

        [Required]
        [Range(10, 100)]
        public int Capacity { get; set; } // Total number of seats

        public string? Amenities { get; set; } // e.g., "WiFi, AC, Charging Ports"

        public bool IsActive { get; set; } = true;

        // Navigation Property
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
