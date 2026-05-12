using System.ComponentModel.DataAnnotations;

namespace Second_Try.Models
{
    public enum EmployeeRole
    {
        Admin,
        Standard
    }

    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public EmployeeRole Role { get; set; } = EmployeeRole.Standard;

        public bool IsActive { get; set; } = true;

        public string? ProfilePictureUrl { get; set; }

        public string? CoverPictureUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
