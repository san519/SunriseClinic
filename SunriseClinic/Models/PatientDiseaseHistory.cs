using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SunriseClinic.Models
{
    public class PatientDiseaseHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        [StringLength(200)]
        public string DiseaseName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Symptoms { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DiagnosisDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? RecoveryDate { get; set; }

        [StringLength(50)]
        public string? Status { get; set; } // Active, Recovered, Chronic, etc.

        [StringLength(1000)]
        public string? TreatmentDetails { get; set; }

        [StringLength(500)]
        public string? Medications { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

    }
}