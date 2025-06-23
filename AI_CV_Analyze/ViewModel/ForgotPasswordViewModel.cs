using System.ComponentModel.DataAnnotations;

namespace AI_CV_Analyze.ViewModel
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
} 