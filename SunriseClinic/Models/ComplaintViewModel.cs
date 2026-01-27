using System;
using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class ComplaintViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters")]
        public string VisitorName { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string VisitorEmail { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string VisitorPhone { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        public string ComplaintType { get; set; } = "Complaint";
    }
}