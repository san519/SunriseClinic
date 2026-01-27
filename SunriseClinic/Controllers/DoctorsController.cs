using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SunriseClinic.Models;
using System.Data;

namespace SunriseClinic.Controllers
{
    public class DoctorsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DoctorsController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        // GET: /Doctors
        public IActionResult Index()
        {
            try
            {
                var doctors = GetDoctorsFromDatabase();
                return View(doctors); // Pass as model instead of ViewBag
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error loading doctors: {ex.Message}");
                return View(new List<Doctor>());
            }
        }

        // GET: /Doctors/Details/{id}
        public IActionResult Details(int id)
        {
            try
            {
                var doctor = GetDoctorById(id);
                return doctor != null ? View(doctor) : NotFound();
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        private Doctor GetDoctorById(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string query = @"
                SELECT u.UserId, u.FullName, u.Email, u.PhoneNumber, 
                       ISNULL(u.ProfilePicture, 'doctor-default.jpg') as ProfilePicture,
                       ISNULL(d.Specialization, 'General Physician') as Specialization,
                       ISNULL(d.Qualification, 'MBBS') as Qualification,
                       ISNULL(d.ConsultationFee, 500.00) as ConsultationFee,
                       ISNULL(d.AvailableDays, 'Mon-Fri') as AvailableDays,
                       ISNULL(d.AvailableTime, '9:00 AM - 5:00 PM') as AvailableTime,
                       ISNULL(d.ExperienceYears, 5) as ExperienceYears,
                       ISNULL(d.Department, 'General Medicine') as Department,
                       ISNULL(d.LicenseNumber, 'BMDC-XXXXX') as LicenseNumber
                FROM Users u
                LEFT JOIN Doctors d ON u.UserId = d.DoctorId
                WHERE u.UserId = @DoctorId 
                  AND u.UserType = 'Doctor' 
                  AND u.IsActive = 1";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@DoctorId", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Doctor
                {
                    DoctorId = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Email = reader.GetString(2),
                    PhoneNumber = reader.GetString(3),
                    ProfilePicture = reader.GetString(4),
                    Specialization = reader.GetString(5),
                    Qualification = reader.GetString(6),
                    ConsultationFee = reader.GetDecimal(7),
                    AvailableDays = reader.GetString(8),
                    AvailableTime = reader.GetString(9),
                    ExperienceYears = reader.GetInt32(10),
                    Department = reader.GetString(11),
                    LicenseNumber = reader.GetString(12),
                    DisplayId = "D" + (reader.GetInt32(0) + 9000)
                };
            }
            return null;
        }

        private List<Doctor> GetDoctorsFromDatabase()
        {
            var doctors = new List<Doctor>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string query = @"
                SELECT u.UserId, u.FullName, u.Email, u.PhoneNumber,
                       ISNULL(u.ProfilePicture, 'doctor-default.jpg') as ProfilePicture,
                       ISNULL(d.Specialization, 'General Physician') as Specialization,
                       ISNULL(d.Qualification, 'MBBS') as Qualification,
                       ISNULL(d.ConsultationFee, 500.00) as ConsultationFee,
                       ISNULL(d.AvailableDays, 'Mon-Fri') as AvailableDays,
                       ISNULL(d.AvailableTime, '9:00 AM - 5:00 PM') as AvailableTime,
                       ISNULL(d.ExperienceYears, 5) as ExperienceYears,
                       ISNULL(d.Department, 'General Medicine') as Department,
                       ISNULL(d.LicenseNumber, 'BMDC-XXXXX') as LicenseNumber
                FROM Users u
                LEFT JOIN Doctors d ON u.UserId = d.DoctorId
                WHERE u.UserType = 'Doctor' AND u.IsActive = 1
                ORDER BY u.FullName";

            using var cmd = new SqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                doctors.Add(new Doctor
                {
                    DoctorId = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Email = reader.GetString(2),
                    PhoneNumber = reader.GetString(3),
                    ProfilePicture = reader.GetString(4),
                    Specialization = reader.GetString(5),
                    Qualification = reader.GetString(6),
                    ConsultationFee = reader.GetDecimal(7),
                    AvailableDays = reader.GetString(8),
                    AvailableTime = reader.GetString(9),
                    ExperienceYears = reader.GetInt32(10),
                    Department = reader.GetString(11),
                    LicenseNumber = reader.GetString(12),
                    DisplayId = "D" + (reader.GetInt32(0) + 9000)
                });
            }

            return doctors;
        }
    }
}