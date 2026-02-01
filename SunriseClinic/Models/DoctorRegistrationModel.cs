using System;
using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class DoctorRegistrationModel
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
        [Display(Name = "Password")]
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
        [Display(Name = "Gender")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }

        // Doctor Specific Fields
        [Required(ErrorMessage = "Specialization is required")]
        [Display(Name = "Specialization")]
        public string Specialization { get; set; }

        [Required(ErrorMessage = "Qualification is required")]
        [Display(Name = "Qualification")]
        public string Qualification { get; set; }

        [Required(ErrorMessage = "License number is required")]
        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; }

        [Required(ErrorMessage = "Consultation fee is required")]
        [Range(0, 100000, ErrorMessage = "Consultation fee must be between 0 and 100000")]
        [Display(Name = "Consultation Fee")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Enter a valid amount (e.g., 500 or 500.50)")]
        [DisplayFormat(DataFormatString = "{0:0.##}", ApplyFormatInEditMode = true)]
        public decimal ConsultationFee { get; set; }

        [Required(ErrorMessage = "Available days are required")]
        [Display(Name = "Available Days")]
        public string AvailableDays { get; set; }

        [Required(ErrorMessage = "Available time is required")]
        [Display(Name = "Available Time")]
        public string AvailableTime { get; set; }

        [Display(Name = "Experience (Years)")]
        public int ExperienceYears { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }
    }
}