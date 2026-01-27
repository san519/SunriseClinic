using System;
using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class NurseUpdateModel
    {
        // UserId ফর্ম থেকে পাঠানো হবে না, সেশন থেকে নেয়া হবে
        public int UserId { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "Must be a valid Bangladeshi phone number (01XXXXXXXXX)")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [Display(Name = "Gender")]
        public string Gender { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
        [Display(Name = "Department")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Shift time is required")]
        [StringLength(50, ErrorMessage = "Shift time cannot exceed 50 characters")]
        [Display(Name = "Shift Time")]
        public string ShiftTime { get; set; }

        [Required(ErrorMessage = "Experience years is required")]
        [Range(0, 50, ErrorMessage = "Experience must be between 0 and 50 years")]
        [Display(Name = "Experience (Years)")]
        public int ExperienceYears { get; set; }
    }
}