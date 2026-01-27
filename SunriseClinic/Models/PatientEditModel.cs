using System;
using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class PatientEditModel
    {
        public int UserId { get; set; }

        // ✅ Database: Users(FullName) - NOT NULL
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters")]
        public string FullName { get; set; }

        // ✅ Database: Users(Email) - NOT NULL
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        // ✅ Database: Users(DateOfBirth) - NULL
        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        // ✅ Database: Users(Gender) - NULL (CHECK constraint আছে)
        [Required(ErrorMessage = "Gender is required")]
        [StringLength(1, ErrorMessage = "Gender must be 1 character")]
        [RegularExpression("^[MFO]$", ErrorMessage = "Gender must be M, F, or O")]
        public string Gender { get; set; }

        // ✅ Database: Users(PhoneNumber) - NULL
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        // ✅ Database: Users(Address) - NULL
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }

        // ✅ Database: Patients(BloodGroup) - NULL
        [StringLength(5, ErrorMessage = "Blood group cannot exceed 5 characters")]
        [Display(Name = "Blood Group")]
        public string BloodGroup { get; set; }

        // ✅ Database: Patients(Height) - NULL
        [Display(Name = "Height (cm)")]
        [Range(0, 300, ErrorMessage = "Height must be between 0 and 300 cm")]
        public decimal? Height { get; set; }

        // ✅ Database: Patients(Weight) - NULL
        [Display(Name = "Weight (kg)")]
        [Range(0, 500, ErrorMessage = "Weight must be between 0 and 500 kg")]
        public decimal? Weight { get; set; }

        // ✅ Database: Patients(EmergencyContact) - NULL
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Emergency Contact")]
        public string EmergencyContact { get; set; }

        // ✅ Database: Patients(InsuranceInfo) - NULL
        [Display(Name = "Insurance Info")]
        [StringLength(500, ErrorMessage = "Insurance info cannot exceed 500 characters")]
        public string InsuranceInfo { get; set; }

        // ✅ Database: Patients(Occupation) - NULL
        [StringLength(100, ErrorMessage = "Occupation cannot exceed 100 characters")]
        public string Occupation { get; set; }

        // ✅ Database: Patients(MaritalStatus) - NULL
        [StringLength(20, ErrorMessage = "Marital status cannot exceed 20 characters")]
        [Display(Name = "Marital Status")]
        public string MaritalStatus { get; set; }

        // ✅ Database: Users(IsActive) - NOT NULL (DEFAULT 1)
        [Required(ErrorMessage = "Account status is required")]
        [Display(Name = "Account Status")]
        public bool IsActive { get; set; } = true;

        // ✅ Display only - NOT in form submission
        public string DisplayId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}