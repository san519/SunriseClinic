using System;
using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class Complaint
    {
        public int ComplaintId { get; set; }

        public int? PatientId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters")]
        public string VisitorName { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string VisitorEmail { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        public DateTime ComplaintDate { get; set; }
        public bool IsImportant { get; set; }
        public bool IsResolved { get; set; }
        public string AdminNotes { get; set; }

        // For display
        public string PatientName { get; set; }
    }
}