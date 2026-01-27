using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SunriseClinic.Models
{
    public class AppointmentViewModel
    {
        [Required(ErrorMessage = "Please select a doctor")]
        [Display(Name = "Select Doctor")]
        public int SelectedDoctorId { get; set; }

        [Required(ErrorMessage = "Please select a date")]
        [DataType(DataType.Date)]
        [Display(Name = "Appointment Date")]
        public DateTime SelectedDate { get; set; }

        [Required(ErrorMessage = "Please select a time slot")]
        [Display(Name = "Preferred Time")]
        public string SelectedTimeSlot { get; set; }

        [Required(ErrorMessage = "Please describe the reason for appointment")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        [Display(Name = "Reason for Appointment")]
        public string Reason { get; set; }

        [Display(Name = "Any specific symptoms?")]
        public string Symptoms { get; set; }

        [Display(Name = "Is this an emergency?")]
        public bool IsEmergency { get; set; } = false;

        // Available doctors for dropdown (Not required in POST)
        [NotMapped]
        public List<Doctor> AvailableDoctors { get; set; }

        // Available time slots (Not required in POST)
        [NotMapped]
        public List<string> TimeSlots { get; set; }
    }
}