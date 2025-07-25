using System.ComponentModel.DataAnnotations;

namespace AI_CV_Analyze.ViewModel
{
    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string OTP { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Confirmation password does not match.")]
        public string ConfirmPassword { get; set; }
    }
} 