using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class RegisterWithVerificationModel : PatientRegistrationModel
    {
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
        [Display(Name = "Verification Code")]
        public string VerificationCode { get; set; }
    }
}