using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class Doctor
    {
        public int DoctorId { get; set; }

        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "Specialization")]
        public string Specialization { get; set; }

        [Display(Name = "Qualification")]
        public string Qualification { get; set; }

        [Display(Name = "Consultation Fee")]
        public decimal ConsultationFee { get; set; }

        [Display(Name = "Available Days")]
        public string AvailableDays { get; set; }

        [Display(Name = "Available Time")]
        public string AvailableTime { get; set; }

        [Display(Name = "Profile Picture")]
        public string ProfilePicture { get; set; } = "default.jpg";

        [Display(Name = "Experience Years")]
        public int ExperienceYears { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        // For display only
        public string DisplayId { get; set; }
    }
}