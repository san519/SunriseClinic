using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using SunriseClinic.Models;

namespace SunriseClinic.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public AppointmentController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        // Check if user is logged in
        private bool IsLoggedIn()
        {
            var userId = HttpContext.Session.GetString("UserId");
            return !string.IsNullOrEmpty(userId);
        }

        // GET: /Appointment/Create
        public IActionResult Create()
        {
            // Check if user is logged in
            if (!IsLoggedIn())
            {
                HttpContext.Session.SetString("AppointmentLoginRequired", "true");
                return RedirectToAction("Login", "Account", new { returnUrl = "/Appointment/Create" });
            }

            var userId = int.Parse(HttpContext.Session.GetString("UserId"));
            var userType = HttpContext.Session.GetString("UserType");

            // Only patients can book appointments
            if (userType != "Patient")
            {
                TempData["ErrorMessage"] = "Only patients can book appointments";
                return RedirectToAction("Dashboard", userType);
            }

            try
            {
                var model = new AppointmentViewModel
                {
                    SelectedDate = DateTime.Today
                };

                // Load dropdown data
                model.AvailableDoctors = GetAvailableDoctors();
                model.TimeSlots = GetTimeSlots();


                if (model.AvailableDoctors == null || model.AvailableDoctors.Count == 0)
                {
                    Console.WriteLine("⚠️ No doctors available in system!");
                    TempData["ErrorMessage"] = "No doctors are currently available. Please contact administration.";
                }

                ViewBag.PatientProfilePicture = GetPatientProfilePicture(userId);

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading create page: {ex.Message}");
                TempData["ErrorMessage"] = "Failed to load appointment booking page";
                return RedirectToAction("Dashboard", "Patient");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(AppointmentViewModel model)
        {
            Console.WriteLine("=== APPOINTMENT CREATE SUBMISSION ===");

            // Check login
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "Please login first to book an appointment";
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(HttpContext.Session.GetString("UserId"));
            var userType = HttpContext.Session.GetString("UserType");

            if (userType != "Patient")
            {
                TempData["ErrorMessage"] = "Only patients can book appointments";
                return RedirectToAction("Dashboard", userType);
            }

            // Validate only the required fields, not AvailableDoctors and TimeSlots
            bool isValid = true;

            if (model.SelectedDoctorId <= 0)
            {
                ModelState.AddModelError("SelectedDoctorId", "Please select a doctor");
                isValid = false;
            }

            // আজকের তারিখ বা ভবিষ্যতের তারিখ validation
            if (model.SelectedDate < DateTime.Today)  // গতকালের তারিখ হলে error
            {
                ModelState.AddModelError("SelectedDate", "Appointment date cannot be in the past");
                isValid = false;
            }

            // সময় validation - যদি আজকের তারিখ হয়, তাহলে বর্তমান সময়ের পরের সময় select করতে হবে
            if (model.SelectedDate.Date == DateTime.Today.Date)
            {
                TimeSpan selectedTime;
                if (TimeSpan.TryParse(model.SelectedTimeSlot, out selectedTime))
                {
                    DateTime selectedDateTime = model.SelectedDate.Add(selectedTime);
                    if (selectedDateTime <= DateTime.Now.AddMinutes(30)) // অন্তত 30 মিনিট পরের সময়
                    {
                        ModelState.AddModelError("SelectedTimeSlot", "For today's appointment, please select a time at least 30 minutes from now");
                        isValid = false;
                    }
                }
            }

            if (string.IsNullOrEmpty(model.SelectedTimeSlot))
            {
                ModelState.AddModelError("SelectedTimeSlot", "Please select a time slot");
                isValid = false;
            }

            if (string.IsNullOrEmpty(model.Reason))
            {
                ModelState.AddModelError("Reason", "Please provide a reason for appointment");
                isValid = false;
            }

            if (!isValid)
            {
                // Re-initialize dropdown data
                model.AvailableDoctors = GetAvailableDoctors();
                model.TimeSlots = GetTimeSlots();
                return View(model);
            }

                try
                {
                    // Parse time slot
                    TimeSpan appointmentTime;
                    if (!TimeSpan.TryParse(model.SelectedTimeSlot, out appointmentTime))
                    {
                        ModelState.AddModelError("SelectedTimeSlot", "Invalid time format");
                        model.AvailableDoctors = GetAvailableDoctors();
                        model.TimeSlots = GetTimeSlots();
                        return View(model);
                    }

                    // **Debug: Check IsEmergency value**
                    Console.WriteLine($"IsEmergency value from form: {model.IsEmergency}");

                    // Create appointment
                    int appointmentId;
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        var cmd = new SqlCommand(@"
                INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, AppointmentTime, 
                                         Status, Reason, Symptoms, IsEmergency, CreatedAt)
                OUTPUT INSERTED.AppointmentId
                VALUES (@PatientId, @DoctorId, @AppointmentDate, @AppointmentTime, 
                        'Pending', @Reason, @Symptoms, @IsEmergency, GETDATE())",
                                connection);

                        cmd.Parameters.AddWithValue("@PatientId", userId);
                        cmd.Parameters.AddWithValue("@DoctorId", model.SelectedDoctorId);
                        cmd.Parameters.AddWithValue("@AppointmentDate", model.SelectedDate);
                        cmd.Parameters.AddWithValue("@AppointmentTime", appointmentTime);
                        cmd.Parameters.AddWithValue("@Reason", model.Reason ?? "");
                        cmd.Parameters.AddWithValue("@Symptoms", model.Symptoms ?? "");
                        cmd.Parameters.AddWithValue("@IsEmergency", model.IsEmergency); // **এইটা ঠিকমত সেট করুন**

                        var result = cmd.ExecuteScalar();
                        appointmentId = result != null ? Convert.ToInt32(result) : 0;

                        Console.WriteLine($"✅ Appointment created successfully! ID: {appointmentId}, Emergency: {model.IsEmergency}");
                    }

                    TempData["SuccessMessage"] = $"✅ Appointment booked successfully! Your Appointment ID: AP{appointmentId:D6}";
                    return RedirectToAction("MyAppointments");
                }
                catch (SqlException sqlEx)
                {
                    Console.WriteLine($"❌ SQL Error: {sqlEx.Message}");
                    TempData["ErrorMessage"] = "Database error occurred. Please try again.";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    TempData["ErrorMessage"] = "Failed to book appointment. Please try again.";
                }

                // If error occurred, reload dropdown data and return to view
                model.AvailableDoctors = GetAvailableDoctors();
                model.TimeSlots = GetTimeSlots();
                return View(model);
            }

        // GET: /Appointment/MyAppointments
        public IActionResult MyAppointments()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = int.Parse(HttpContext.Session.GetString("UserId"));
            var userType = HttpContext.Session.GetString("UserType");

            List<Appointment> appointments;

            if (userType == "Patient")
            {
                appointments = GetPatientAppointments(userId);
                ViewBag.UserType = "Patient";
            }
            else if (userType == "Doctor")
            {
                appointments = GetDoctorAppointments(userId);
                ViewBag.UserType = "Doctor";
            }
            else
            {
                TempData["ErrorMessage"] = "You don't have permission to view appointments";
                return RedirectToAction("Dashboard", userType);
            }

            ViewBag.Appointments = appointments;
            ViewBag.PatientProfilePicture = GetPatientProfilePicture(userId);
            return View();
        }

        // GET: /Appointment/Details/{id}
        public IActionResult Details(int id)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = int.Parse(HttpContext.Session.GetString("UserId"));
            var userType = HttpContext.Session.GetString("UserType");

            var appointment = GetAppointmentById(id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found";
                return RedirectToAction("MyAppointments");
            }

            // Check permission
            if (userType == "Patient" && appointment.PatientId != userId)
            {
                TempData["ErrorMessage"] = "You don't have permission to view this appointment";
                return RedirectToAction("MyAppointments");
            }

            if (userType == "Doctor" && appointment.DoctorId != userId)
            {
                TempData["ErrorMessage"] = "You don't have permission to view this appointment";
                return RedirectToAction("MyAppointments");
            }

            // ✅ **Profile picture ViewBag এ সেট করুন**
            if (userType == "Patient")
            {
                ViewBag.PatientProfilePicture = GetPatientProfilePicture(userId);
                ViewBag.UserType = "Patient";
            }
            else if (userType == "Doctor")
            {
                ViewBag.DoctorProfilePicture = GetDoctorProfilePicture(userId);
                ViewBag.UserType = "Doctor";
            }

            return View(appointment);
        }

        // POST: /Appointment/Cancel/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = int.Parse(HttpContext.Session.GetString("UserId"));
            var userType = HttpContext.Session.GetString("UserType");

            var appointment = GetAppointmentById(id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found";
                return RedirectToAction("MyAppointments");
            }

            // Check permission (only patient can cancel)
            if (userType != "Patient" || appointment.PatientId != userId)
            {
                TempData["ErrorMessage"] = "You don't have permission to cancel this appointment";
                return RedirectToAction("MyAppointments");
            }

            // Check if appointment can be cancelled (at least 2 hours before)
            var appointmentDateTime = appointment.AppointmentDate.Add(appointment.AppointmentTime);
            if (appointmentDateTime < DateTime.Now.AddHours(2))
            {
                TempData["ErrorMessage"] = "Appointment can only be cancelled at least 2 hours before the scheduled time";
                return RedirectToAction("Details", new { id = id });
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var cmd = new SqlCommand(
                        "UPDATE Appointments SET Status = 'Cancelled', UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy WHERE AppointmentId = @AppointmentId",
                        connection);

                    cmd.Parameters.AddWithValue("@AppointmentId", id);
                    cmd.Parameters.AddWithValue("@UpdatedBy", userId);

                    cmd.ExecuteNonQuery();

                    // Create notification
                    CreateNotification(userId, "Appointment Cancelled",
                        $"Your appointment for {appointment.AppointmentDate.ToString("dd-MMM-yyyy")} has been cancelled",
                        id);

                    TempData["SuccessMessage"] = "Appointment cancelled successfully";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to cancel appointment. Please try again.";
                Console.WriteLine($"Cancel appointment error: {ex.Message}");
            }

            return RedirectToAction("MyAppointments");
        }

        // Helper methods

        // Helper method to get doctor profile picture
        private string GetDoctorProfilePicture(int userId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "SELECT ProfilePicture FROM Users WHERE UserId = @UserId",
                        connection);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "default.jpg";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting doctor profile picture: {ex.Message}");
                return "default.jpg";
            }
        }

        // PatientController.cs - Add this helper method
        private string GetPatientProfilePicture(int userId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "SELECT ProfilePicture FROM Users WHERE UserId = @UserId",
                        connection);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "default.jpg";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting profile picture: {ex.Message}");
                return "default.jpg";
            }
        }

        private List<Doctor> GetAvailableDoctors()
        {
            var doctors = new List<Doctor>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
                    SELECT u.UserId, u.FullName, d.Specialization, d.Qualification, 
                           d.ConsultationFee, d.AvailableDays, d.AvailableTime
                    FROM Users u
                    INNER JOIN Doctors d ON u.UserId = d.DoctorId
                    WHERE u.UserType = 'Doctor' AND u.IsActive = 1
                    ORDER BY u.FullName",
                    connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        doctors.Add(new Doctor
                        {
                            DoctorId = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            Specialization = reader.GetString(2),
                            Qualification = reader.GetString(3),
                            ConsultationFee = reader.GetDecimal(4),
                            AvailableDays = reader.GetString(5),
                            AvailableTime = reader.GetString(6)
                        });
                    }
                }
            }

            return doctors;
        }

        private List<string> GetTimeSlots()
        {
            // Generate time slots from 9 AM to 5 PM, every 30 minutes
            var timeSlots = new List<string>();
            var startTime = new TimeSpan(9, 0, 0); // 9:00 AM
            var endTime = new TimeSpan(17, 0, 0);  // 5:00 PM

            while (startTime <= endTime)
            {
                timeSlots.Add(startTime.ToString(@"hh\:mm"));
                startTime = startTime.Add(TimeSpan.FromMinutes(30));
            }

            return timeSlots;
        }

        private bool IsDoctorAvailable(int doctorId, DateTime date, TimeSpan time)
        {
            // Check if doctor has any appointment at the same time
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM Appointments 
                    WHERE DoctorId = @DoctorId 
                    AND AppointmentDate = @AppointmentDate 
                    AND AppointmentTime = @AppointmentTime 
                    AND Status IN ('Pending', 'Approved')",
                    connection);

                cmd.Parameters.AddWithValue("@DoctorId", doctorId);
                cmd.Parameters.AddWithValue("@AppointmentDate", date);
                cmd.Parameters.AddWithValue("@AppointmentTime", time);

                var count = (int)cmd.ExecuteScalar();
                return count == 0; // Available if no appointment at that time
            }
        }

        private List<Appointment> GetPatientAppointments(int patientId)
        {
            var appointments = new List<Appointment>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
                    SELECT 
                        a.AppointmentId,
                        a.PatientId,
                        a.DoctorId,
                        a.AppointmentDate,
                        a.AppointmentTime,
                        a.Status,
                        a.Reason,
                        a.Symptoms,
                        a.IsEmergency,
                        a.CreatedAt,
                        u.FullName AS DoctorName,
                        d.Specialization AS DoctorSpecialization
                    FROM Appointments a
                    INNER JOIN Users u ON a.DoctorId = u.UserId
                    LEFT JOIN Doctors d ON a.DoctorId = d.DoctorId
                    WHERE a.PatientId = @PatientId
                    ORDER BY a.AppointmentDate DESC, a.AppointmentTime DESC",
                    connection);

                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new Appointment
                        {
                            AppointmentId = reader.GetInt32(0),
                            PatientId = reader.GetInt32(1),
                            DoctorId = reader.GetInt32(2),
                            AppointmentDate = reader.GetDateTime(3),
                            AppointmentTime = reader.GetTimeSpan(4),
                            Status = reader.GetString(5),
                            Reason = reader.GetString(6),
                            Symptoms = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            CreatedAt = reader.GetDateTime(9),
                            DoctorName = reader.GetString(10),
                            DoctorSpecialization = reader.GetString(11)
                        });
                    }
                }
            }

            return appointments;
        }

        private List<Appointment> GetDoctorAppointments(int doctorId)
        {
            var appointments = new List<Appointment>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
                    SELECT 
                        a.AppointmentId,
                        a.PatientId,
                        a.DoctorId,
                        a.AppointmentDate,
                        a.AppointmentTime,
                        a.Status,
                        a.Reason,
                        a.Symptoms,
                        a.IsEmergency,
                        a.CreatedAt,
                        u.FullName AS PatientName
                    FROM Appointments a
                    INNER JOIN Users u ON a.PatientId = u.UserId
                    WHERE a.DoctorId = @DoctorId
                    ORDER BY a.AppointmentDate, a.AppointmentTime",
                    connection);

                cmd.Parameters.AddWithValue("@DoctorId", doctorId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new Appointment
                        {
                            AppointmentId = reader.GetInt32(0),
                            PatientId = reader.GetInt32(1),
                            DoctorId = reader.GetInt32(2),
                            AppointmentDate = reader.GetDateTime(3),
                            AppointmentTime = reader.GetTimeSpan(4),
                            Status = reader.GetString(5),
                            Reason = reader.GetString(6),
                            Symptoms = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            CreatedAt = reader.GetDateTime(9),
                            PatientName = reader.GetString(10)
                        });
                    }
                }
            }

            return appointments;
        }

        private Appointment GetAppointmentById(int appointmentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
            SELECT a.AppointmentId, a.PatientId, a.DoctorId, a.AppointmentDate, 
                   a.AppointmentTime, a.Status, a.Reason, a.Symptoms, a.IsEmergency,
                   a.CreatedAt, a.UpdatedAt,
                   u1.FullName AS PatientName, u2.FullName AS DoctorName, 
                   d.Specialization AS DoctorSpecialization
            FROM Appointments a
            INNER JOIN Users u1 ON a.PatientId = u1.UserId
            INNER JOIN Doctors doc ON a.DoctorId = doc.DoctorId
            INNER JOIN Users u2 ON doc.DoctorId = u2.UserId
            INNER JOIN Doctors d ON a.DoctorId = d.DoctorId
            WHERE a.AppointmentId = @AppointmentId",
                    connection);

                cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Appointment
                        {
                            AppointmentId = reader.GetInt32(0),
                            PatientId = reader.GetInt32(1),
                            DoctorId = reader.GetInt32(2),
                            AppointmentDate = reader.GetDateTime(3),
                            AppointmentTime = reader.GetTimeSpan(4),
                            Status = reader.GetString(5),
                            Reason = reader.GetString(6),
                            Symptoms = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            IsEmergency = reader.GetBoolean(8), // **এই লাইনটা যোগ করুন**
                            CreatedAt = reader.GetDateTime(9),
                            UpdatedAt = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10),
                            PatientName = reader.GetString(11),
                            DoctorName = reader.GetString(12),
                            DoctorSpecialization = reader.GetString(13)
                        };
                    }
                }
            }

            return null;
        }

        private void CreateNotification(int userId, string title, string message, int? relatedAppointmentId = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var cmd = new SqlCommand(@"
                        INSERT INTO Notifications (UserId, Title, Message, IsRead, CreatedAt, RelatedAppointmentId)
                        VALUES (@UserId, @Title, @Message, 0, GETDATE(), @RelatedAppointmentId)",
                        connection);

                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Title", title);
                    cmd.Parameters.AddWithValue("@Message", message);

                    if (relatedAppointmentId.HasValue)
                        cmd.Parameters.AddWithValue("@RelatedAppointmentId", relatedAppointmentId.Value);
                    else
                        cmd.Parameters.AddWithValue("@RelatedAppointmentId", DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create notification error: {ex.Message}");
            }
        }
    }
}