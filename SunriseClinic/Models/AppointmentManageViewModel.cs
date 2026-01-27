using System;
using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class AppointmentManageViewModel
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Appointment date is required")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Appointment time is required")]
        public string AppointmentTime { get; set; }

        public TimeSpan AppointmentTimeSpan { get; set; }

        public string Status { get; set; } = "Pending";

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; }

        public string Symptoms { get; set; }

        [Display(Name = "Is this an emergency?")]
        public bool IsEmergency { get; set; } = false;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Patient Information
        public string PatientName { get; set; }
        public string PatientPhone { get; set; }
        public string PatientEmail { get; set; }
        public DateTime? PatientDOB { get; set; }
        public string PatientGender { get; set; }
        public string BloodGroup { get; set; }
        public string EmergencyContact { get; set; }
        public decimal Height { get; set; }
        public decimal Weight { get; set; }

        // Doctor Information
        public string DoctorName { get; set; }
        public string DoctorSpecialization { get; set; }
        public string Qualification { get; set; }
        public decimal ConsultationFee { get; set; }
        public string AvailableDays { get; set; }
        public string AvailableTime { get; set; }
        public int ExperienceYears { get; set; }

        // Helper properties
        public string EmergencyStatusText => IsEmergency ? "EMERGENCY" : "Regular";

        public string StatusBadgeClass =>
            Status switch
            {
                "Approved" => "badge bg-success",
                "Pending" => "badge bg-warning",
                "Cancelled" => "badge bg-danger",
                "Completed" => "badge bg-info",
                _ => "badge bg-secondary"
            };

        public string FormattedDateTime =>
            $"{AppointmentDate:dd-MMM-yyyy} at {AppointmentTimeSpan:hh\\:mm}";

        public string FormattedPatientDOB =>
            PatientDOB.HasValue ? PatientDOB.Value.ToString("dd MMM yyyy") : "Not specified";
    }
}