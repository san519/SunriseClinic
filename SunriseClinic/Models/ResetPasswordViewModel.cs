using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Reset code is required")]
        [Display(Name = "Reset Code")]
        public string ResetCode { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Email { get; set; }
    }
}