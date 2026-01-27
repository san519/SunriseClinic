using System;
using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class NurseProfileViewModel
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(150)]
        public string FullName { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(1)]
        public string Gender { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [StringLength(255)]
        public string Address { get; set; }

        public string ProfilePicture { get; set; }

        [Required]
        [StringLength(100)]
        public string Department { get; set; }

        [Required]
        [StringLength(50)]
        public string ShiftTime { get; set; }

        [Required]
        [StringLength(50)]
        public string NurseLicense { get; set; }

        [Range(0, 50)]
        public int ExperienceYears { get; set; }

        // Display only fields
        public string DisplayId { get; set; }
        public string Username { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}