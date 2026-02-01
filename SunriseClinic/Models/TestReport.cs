using System;
using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class TestReport
    {
        public int ReportId { get; set; }
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Report name is required")]
        [Display(Name = "Report Name")]
        public string ReportName { get; set; }

        [Required(ErrorMessage = "Report date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Report Date")]
        public DateTime ReportDate { get; set; }

        [Display(Name = "Report File")]
        public string ReportFile { get; set; }

        public int UploadedBy { get; set; }

        [Display(Name = "Uploaded At")]
        public DateTime UploadedAt { get; set; }

        public string Notes { get; set; }

        // For display only (not mapped to database)
        [Display(Name = "Patient Name")]
        public string PatientName { get; set; }

        [Display(Name = "Uploaded By")]
        public string UploadedByName { get; set; }

        [Display(Name = "File Available")]
        public bool FileExists => !string.IsNullOrEmpty(ReportFile);
    }
}