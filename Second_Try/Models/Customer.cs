using System.ComponentModel.DataAnnotations;

namespace Second_Try.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string GoogleId { get; set; } = string.Empty; // Unique ID from Google OAuth

        // Nullable: Only set for email/password registrations. Google users don't need it.
        public string? PasswordHash { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public string? ProfilePictureUrl { get; set; }

        public string? CoverPictureUrl { get; set; } // Custom profile banner/cover photo

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Loyalty Program & Elite Status
        public bool IsElite { get; set; } = false;
        public int LoyaltyPoints { get; set; } = 0;
        public DateTime? EliteJoinDate { get; set; }

        // Navigation Properties
        public ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
    }
}
