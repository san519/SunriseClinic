using System.ComponentModel.DataAnnotations;

namespace SunriseClinic.Models
{
    public class TestReportUploadModel
    {
        [Required]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Report name is required")]
        [Display(Name = "Report Name")]
        [StringLength(200)]
        public string ReportName { get; set; }

        [Required(ErrorMessage = "Report date is required")]
        [Display(Name = "Report Date")]
        [DataType(DataType.Date)]
        public DateTime ReportDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Please select a report file")]
        [Display(Name = "Report File")]
        public IFormFile ReportFile { get; set; }

        [Display(Name = "Notes")]
        [StringLength(1000)]
        public string Notes { get; set; }
    }
}