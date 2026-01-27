using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SunriseClinic.Models
{
    public class PatientProfileViewModel
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

        [StringLength(5)]
        public string BloodGroup { get; set; }

        [Range(0, 300)]
        public decimal? Height { get; set; }

        [Range(0, 500)]
        public decimal? Weight { get; set; }

        [Phone]
        public string EmergencyContact { get; set; }

        [StringLength(255)]
        public string InsuranceInfo { get; set; }

        [StringLength(100)]
        public string Occupation { get; set; }

        [StringLength(20)]
        public string MaritalStatus { get; set; }

        // Display only fields
        public string DisplayId { get; set; }
        public string Username { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}