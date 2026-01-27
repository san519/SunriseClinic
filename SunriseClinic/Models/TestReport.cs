using System;
using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class TestReport
    {
        public int ReportId { get; set; }
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Report name is required")]
        public string ReportName { get; set; }

        [Required(ErrorMessage = "Report date is required")]
        [DataType(DataType.Date)]
        public DateTime ReportDate { get; set; }

        public string ReportFile { get; set; }
        public int UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
        public string Notes { get; set; }

        // For display
        public string PatientName { get; set; }
        public string UploadedByName { get; set; }
        public bool FileExists { get; set; }
    }
}