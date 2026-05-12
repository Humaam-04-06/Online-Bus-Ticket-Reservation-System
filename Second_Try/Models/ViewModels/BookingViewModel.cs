using System.ComponentModel.DataAnnotations;
using Second_Try.Models; // To access BusType enum

namespace Second_Try.Models.ViewModels
{
    public class BookingViewModel
    {
        public int RouteId { get; set; }
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime TravelDate { get; set; }
        
        [Required]
        public BusType PreferredBusType { get; set; }
        
        [Required]
        [Range(1, 10, ErrorMessage = "You can only book between 1 and 10 seats per request.")]
        public int NumberOfSeats { get; set; } = 1;

        public decimal EstimatedTotalFare { get; set; }
    }
}
