// Models/DoctorEditModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class DoctorEditModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

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
        [Range(0, 100000, ErrorMessage = "Invalid fee amount")]
        [Display(Name = "Consultation Fee")]
        public decimal ConsultationFee { get; set; }

        [Required(ErrorMessage = "Available days are required")]
        [Display(Name = "Available Days")]
        public string AvailableDays { get; set; }

        [Required(ErrorMessage = "Available time is required")]
        [Display(Name = "Available Time")]
        public string AvailableTime { get; set; }

        [Display(Name = "Experience Years")]
        [Range(0, 50, ErrorMessage = "Experience must be between 0 and 50 years")]
        public int ExperienceYears { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }
    }
}