using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email or Patient ID is required")]
        [Display(Name = "Email / Patient ID")]
        public string EmailOrUsername { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}