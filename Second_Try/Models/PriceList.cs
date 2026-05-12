using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Second_Try.Models
{
    public class PriceList
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RouteId { get; set; }

        [Required]
        public BusType BusType { get; set; } // Fare changes based on bus class (Economy, Luxury, etc.)

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FareAmount { get; set; }

        // Navigation Property
        [ForeignKey(nameof(RouteId))]
        public Route? Route { get; set; }
    }
}
