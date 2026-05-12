using System.ComponentModel.DataAnnotations;

namespace Second_Try.Models
{
    public class Route
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Origin { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Destination { get; set; } = string.Empty;

        [Required]
        public double EstimatedDurationHours { get; set; } // e.g., 2.5 hours

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public ICollection<PriceList> PriceLists { get; set; } = new List<PriceList>();
        public ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();
    }
}
