// PrescriptionUploadModel.cs
using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class PrescriptionUploadModel
    {
        [Required(ErrorMessage = "Please select a patient")]
        [Display(Name = "Patient")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Prescription date is required")]
        [Display(Name = "Prescription Date")]
        [DataType(DataType.Date)]
        public DateTime PrescriptionDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Please select a prescription file")]
        [Display(Name = "Prescription File")]
        public IFormFile PrescriptionFile { get; set; }

        [Display(Name = "Notes (Optional)")]
        [StringLength(1000)]
        public string Notes { get; set; }
    }
}

// PrescriptionViewModel.cs
namespace SunriseClinic.Models
{
    public class PrescriptionViewModel
    {
        public int PrescriptionId { get; set; }
        public int PatientId { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public string PrescriptionFile { get; set; }
        public int? FileSize { get; set; }
        public string FileType { get; set; }
        public int PrescribedBy { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }
        public DateTime UploadedAt { get; set; }

        // For display
        public string PrescribedByName { get; set; }
        public string PatientName { get; set; }
        public string DisplayId { get; set; }

        // Helper properties
        public string FormattedDate => PrescriptionDate.ToString("dd-MMM-yyyy");
        public string FormattedFileSize => FileSize.HasValue ?
            (FileSize.Value < 1024 * 1024 ?
                $"{FileSize.Value / 1024} KB" :
                $"{FileSize.Value / (1024 * 1024):0.0} MB") :
            "N/A";
        public bool IsImage => FileType?.ToLower() is "jpg" or "jpeg" or "png" or "webp";
        public bool IsPdf => FileType?.ToLower() == "pdf";
    }
}