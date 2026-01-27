namespace SunriseClinic.Models
{
    public class DoctorViewModel
    {
        public List<Doctor> Doctors { get; set; }
        public List<string> Specializations { get; set; }
        public List<string> Departments { get; set; }
        public string SelectedSpecialization { get; set; }
        public string SelectedDepartment { get; set; }
        public string SearchQuery { get; set; }
    }
}