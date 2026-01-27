using System;
using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class PatientRegistrationModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^(\+?880|0)1[3-9]\d{8}$",
            ErrorMessage = "Invalid Bangladeshi phone number. Format: +8801XXXXXXXXX or 01XXXXXXXXX")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }

        [Display(Name = "Blood Group")]
        public string BloodGroup { get; set; }

        [RegularExpression(@"^(\+?880|0)1[3-9]\d{8}$",
            ErrorMessage = "Invalid Bangladeshi phone number. Format: +8801XXXXXXXXX or 01XXXXXXXXX")]
        [Display(Name = "Emergency Contact")]
        public string EmergencyContact { get; set; }
    }
}