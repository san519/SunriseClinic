// Models/Appointment.cs - প্রপার্টি যোগ করুন
using System;
using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Appointment date is required")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Appointment time is required")]
        [DataType(DataType.Time)]
        public TimeSpan AppointmentTime { get; set; }

        public string Status { get; set; } = "Pending";

        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; }

        public string Symptoms { get; set; }

        [Display(Name = "Is this an emergency?")]
        public bool IsEmergency { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties (for display)
        public string DoctorName { get; set; }
        public string DoctorSpecialization { get; set; }
        public string PatientName { get; set; }

        // New properties for management view
        public string PatientPhone { get; set; }
        public string PatientEmail { get; set; }
        public DateTime? PatientDOB { get; set; }
        public string PatientGender { get; set; }
        public string BloodGroup { get; set; }
        public string EmergencyContact { get; set; }
        public decimal Height { get; set; }
        public decimal Weight { get; set; }

        // **এই property টা যোগ করুন**
        public string EmergencyStatusText =>
            IsEmergency ? "EMERGENCY" : "Regular";

        // Helper property for badge class
        public string StatusBadgeClass =>
            Status switch
            {
                "Approved" => "badge bg-success",
                "Pending" => "badge bg-warning",
                "Cancelled" => "badge bg-danger",
                "Completed" => "badge bg-info",
                _ => "badge bg-secondary"
            };

        // Helper property for formatted date time
        public string FormattedDateTime =>
            $"{AppointmentDate:dd-MMM-yyyy} at {AppointmentTime:hh\\:mm}";
    }
}