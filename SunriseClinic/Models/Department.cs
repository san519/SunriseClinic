namespace SunriseClinic.Models
{
    public class Department
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string Description { get; set; }
        public int? HeadDoctorId { get; set; }
        public string HeadDoctorName { get; set; }
        public int DoctorCount { get; set; }
    }
}