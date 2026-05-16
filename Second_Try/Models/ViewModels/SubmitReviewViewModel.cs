using System.ComponentModel.DataAnnotations;

namespace Second_Try.Models.ViewModels
{
    public class SubmitReviewViewModel
    {
        [Required]
        public int BookingRequestId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Please enter a comment.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Comment must be at least 10 characters long.")]
        public string Comment { get; set; } = string.Empty;
    }
}
