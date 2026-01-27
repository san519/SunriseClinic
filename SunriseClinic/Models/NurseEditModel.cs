using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class NurseEditModel
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
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Join date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Join Date")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime CreatedAt { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [Display(Name = "Gender")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }

        // Nurse Specific Fields
        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Shift time is required")]
        [Display(Name = "Shift Time")]
        public string ShiftTime { get; set; }

        [Required(ErrorMessage = "Nurse license is required")]
        [Display(Name = "Nurse License Number")]
        public string NurseLicense { get; set; }

        [Display(Name = "Experience (Years)")]
        [Range(0, 50, ErrorMessage = "Experience must be between 0 and 50 years")]
        public int ExperienceYears { get; set; }

        [Display(Name = "Account Status")]
        public bool IsActive { get; set; }
    }
}