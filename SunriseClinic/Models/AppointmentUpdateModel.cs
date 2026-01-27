// Model for appointment update
public class AppointmentUpdateModel
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public string AppointmentDate { get; set; }
    public string AppointmentTime { get; set; }
    public string Status { get; set; } // Approved, Rejected, etc.
    public string Reason { get; set; }
    public string Symptoms { get; set; }
    public bool IsEmergency { get; set; }
}
