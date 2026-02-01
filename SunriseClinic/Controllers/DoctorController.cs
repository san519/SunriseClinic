using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using SunriseClinic.Models;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Security.Claims;


namespace SunriseClinic.Controllers
{
    public class DoctorController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DoctorController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Check if user is logged in as Doctor
        // Check if user is logged in as Doctor
        private bool IsDoctorLoggedIn()
        {
            // ✅ শুধু Cookie-based auth চেক করুন
            return User?.Identity?.IsAuthenticated == true && User.IsInRole("Doctor");
        }

        // Get doctor ID from claims
        private int? GetLoggedDoctorId()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(claimId) && int.TryParse(claimId, out var id))
                {
                    return id;
                }
            }
            return null;
        }



        // GET: /Doctor/Dashboard
        public IActionResult Dashboard()
        {
            if (!IsDoctorLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdBytes = HttpContext.Session.Get("UserId");
            if (userIdBytes == null || !int.TryParse(Encoding.UTF8.GetString(userIdBytes), out int userId))
                return RedirectToAction("Login", "Account");

            try
            {
                // Get doctor details
                var doctor = GetDoctorDetails(userId);
                ViewBag.Doctor = doctor;

                // ✅ Profile Picture Session এ সেট করুন
                if (doctor != null && !string.IsNullOrEmpty(doctor.ProfilePicture))
                {
                    HttpContext.Session.Set("ProfilePicture", Encoding.UTF8.GetBytes(doctor.ProfilePicture));
                }

                // Get today's appointments
                var todaysAppointments = GetTodaysAppointments(userId);
                ViewBag.TodaysAppointments = todaysAppointments;

                // Get upcoming appointments
                var upcomingAppointments = GetUpcomingAppointments(userId);
                ViewBag.UpcomingAppointments = upcomingAppointments;

                // Get statistics
                var stats = GetDoctorStatistics(userId);
                ViewBag.Stats = stats;

                // Get recent patients
                var recentPatients = GetRecentPatients(userId);
                ViewBag.RecentPatients = recentPatients;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Doctor Dashboard Error: {ex.Message}");
                ViewBag.ErrorMessage = "Unable to load dashboard data";
                return View();
            }
        }

        // GET: /Doctor/MyAppointments
        public IActionResult MyAppointments()
        {
            if (!IsDoctorLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdBytes = HttpContext.Session.Get("UserId");
            if (userIdBytes == null || !int.TryParse(Encoding.UTF8.GetString(userIdBytes), out int userId))
                return RedirectToAction("Login", "Account");

            var appointments = GetDoctorAppointments(userId);
            ViewBag.Appointments = appointments;

            return View();
        }

        // GET: /Doctor/PatientDetails/{id}
        public IActionResult PatientDetails(int id)
        {
            if (!IsDoctorLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                var userIdBytes = HttpContext.Session.Get("UserId");
                if (userIdBytes == null || !int.TryParse(Encoding.UTF8.GetString(userIdBytes), out int userId))
                    return RedirectToAction("Login", "Account");

                int patientId = id;

                Console.WriteLine($"DEBUG: Loading patient details for patientId: {patientId}");

                // Get patient basic info
                var patient = GetPatientInfoForDoctor(patientId);
                if (patient == null)
                {
                    TempData["ErrorMessage"] = "Patient not found";
                    return RedirectToAction("Dashboard");
                }

                ViewBag.Patient = patient;

                // Try-catch দিয়ে প্রতিটি মেথড আলাদাভাবে চেক করুন
                try
                {
                    var appointments = GetPatientAppointmentsWithDoctor(patientId, userId);
                    ViewBag.Appointments = appointments;
                    Console.WriteLine($"DEBUG: Appointments loaded successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Error loading appointments: {ex.Message}");
                    ViewBag.Appointments = new List<dynamic>();
                }

                try
                {
                    var medicalHistory = GetPatientMedicalHistory(patientId);
                    ViewBag.MedicalHistory = medicalHistory;
                    Console.WriteLine($"DEBUG: Medical history loaded successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Error loading medical history: {ex.Message}");
                    ViewBag.MedicalHistory = new List<dynamic>();
                }

                try
                {
                    var testReports = GetPatientTestReports(patientId);
                    ViewBag.TestReports = testReports;
                    Console.WriteLine($"DEBUG: Test reports loaded successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Error loading test reports: {ex.Message}");
                    ViewBag.TestReports = new List<dynamic>();
                }

                try
                {
                    var prescriptions = GetPatientPrescriptions(patientId);
                    ViewBag.Prescriptions = prescriptions;
                    Console.WriteLine($"DEBUG: Prescriptions loaded successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Error loading prescriptions: {ex.Message}");
                    Console.WriteLine($"DEBUG: SQL Error details: {ex.InnerException?.Message}");
                    ViewBag.Prescriptions = new List<dynamic>();
                }

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: General error in PatientDetails: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Error loading patient details: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        // এই helper মেথডটি চেক করুন

        private dynamic GetPatientInfoForDoctor(int patientId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT 
                u.UserId,
                u.FullName,
                u.DateOfBirth,
                u.Gender,
                u.PhoneNumber,
                u.Address,
                u.ProfilePicture,
                u.Email,
                p.BloodGroup,
                p.Height,
                p.Weight,
                p.MaritalStatus,
                p.EmergencyContact,
                DATEDIFF(YEAR, u.DateOfBirth, GETDATE()) AS Age
            FROM Users u
            LEFT JOIN Patients p ON u.UserId = p.PatientId
            WHERE u.UserId = @PatientId AND u.UserType = 'Patient' AND u.IsActive = 1",
                    connection);
                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        decimal? height = reader.IsDBNull(9) ? null : reader.GetDecimal(9);
                        decimal? weight = reader.IsDBNull(10) ? null : reader.GetDecimal(10);
                        string bmi = "N/A";

                        if (height.HasValue && weight.HasValue && height.Value > 0)
                        {
                            var heightInM = height.Value / 100;
                            bmi = Math.Round(weight.Value / (heightInM * heightInM), 2).ToString();
                        }

                        return new
                        {
                            UserId = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            DateOfBirth = reader.IsDBNull(2) ? "Not set" : reader.GetDateTime(2).ToString("dd-MMM-yyyy"),
                            Gender = reader.IsDBNull(3) ? "Not set" : reader.GetString(3),
                            PhoneNumber = reader.IsDBNull(4) ? "Not set" : reader.GetString(4),
                            Address = reader.IsDBNull(5) ? "Not set" : reader.GetString(5),
                            ProfilePicture = reader.IsDBNull(6) ? "default.webp" : reader.GetString(6),
                            Email = reader.IsDBNull(7) ? "Not set" : reader.GetString(7),
                            BloodGroup = reader.IsDBNull(8) ? "Not set" : reader.GetString(8),
                            Height = height.HasValue ? $"{height.Value} cm" : "Not set",
                            Weight = weight.HasValue ? $"{weight.Value} kg" : "Not set",
                            MaritalStatus = reader.IsDBNull(11) ? "Not set" : reader.GetString(11),
                            EmergencyContact = reader.IsDBNull(12) ? "Not set" : reader.GetString(12),
                            Age = reader.IsDBNull(13) ? "N/A" : reader.GetInt32(13).ToString(),
                            BMI = bmi
                        };
                    }
                }
            }
            return null;
        }

        // GET: /Doctor/ViewReport/{id}
        public IActionResult ViewReport(int id)
        {
            if (!IsDoctorLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = GetLoggedDoctorId();
            if (userId == null) return RedirectToAction("Login", "Account");
            int doctorId = userId.Value;

            try
            {
                var reportId = id; // ✅ route segment binds to id
                Console.WriteLine($"DEBUG: ViewReport called with reportId: {reportId}");

                var report = GetTestReportDetails(reportId);
                if (report == null)
                {
                    TempData["ErrorMessage"] = "Report not found";
                    return RedirectToAction("Dashboard");
                }

                return View(report);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading report: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }



        // GET: /Doctor/ViewPrescription/{prescriptionId}
        // GET: /Doctor/ViewPrescription/{id}
        public IActionResult ViewPrescription(int id)
        {
            if (!IsDoctorLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdBytes = HttpContext.Session.Get("UserId");
            if (userIdBytes == null || !int.TryParse(Encoding.UTF8.GetString(userIdBytes), out int userId))
                return RedirectToAction("Login", "Account");

            try
            {
                var prescriptionId = id; // ✅ route segment binds to id
                var prescription = GetPrescriptionDetails(prescriptionId);

                if (prescription == null)
                {
                    TempData["ErrorMessage"] = $"Prescription not found for ID: {prescriptionId}";
                    return RedirectToAction("Dashboard");
                }

                return View(prescription);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading prescription: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }



        // GET: /Doctor/Profile
        public IActionResult Profile()
        {
            if (!IsDoctorLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdBytes = HttpContext.Session.Get("UserId");
            if (userIdBytes == null || !int.TryParse(Encoding.UTF8.GetString(userIdBytes), out int userId))
                return RedirectToAction("Login", "Account");

            var doctor = GetDoctorProfile(userId);
            if (doctor == null)
                return RedirectToAction("Login", "Account");

            // ✅ Profile Picture Session এ সেট করুন
            if (!string.IsNullOrEmpty(doctor.ProfilePicture))
            {
                HttpContext.Session.Set("ProfilePicture", Encoding.UTF8.GetBytes(doctor.ProfilePicture));
            }

            return View(doctor);
        }

        // POST: /Doctor/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProfile(DoctorProfileViewModel model)
        {
            if (!IsDoctorLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdBytes = HttpContext.Session.Get("UserId");
            if (userIdBytes == null || !int.TryParse(Encoding.UTF8.GetString(userIdBytes), out int userId))
                return RedirectToAction("Login", "Account");

            // Important: Remove Email validation
            ModelState.Remove("Email");

            // Debug log
            Console.WriteLine("=== UPDATE PROFILE START ===");
            Console.WriteLine($"UserId: {userId}");
            Console.WriteLine($"FullName: {model.FullName}");
            Console.WriteLine($"Department: '{model.Department}'");
            Console.WriteLine($"ExperienceYears: {model.ExperienceYears}");
            Console.WriteLine($"ModelState IsValid: {ModelState.IsValid}");

            if (ModelState.IsValid)
            {
                try
                {
                    UpdateDoctorProfile(userId, model);

                    // Update session
                    HttpContext.Session.Set("UserName", Encoding.UTF8.GetBytes(model.FullName));

                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    Console.WriteLine("Profile update successful");
                    return RedirectToAction("Profile");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating profile: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    ModelState.AddModelError("", $"Error updating profile: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("ModelState errors:");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"- {error.ErrorMessage}");
                }
            }

            return View("Profile", model);
        }

        // GET: /Doctor/MyPatients
        public IActionResult MyPatients()
        {
            if (!IsDoctorLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdBytes = HttpContext.Session.Get("UserId");
            if (userIdBytes == null || !int.TryParse(Encoding.UTF8.GetString(userIdBytes), out int doctorId))
                return RedirectToAction("Login", "Account");

            try
            {
                // Get all unique patients for this doctor
                var patients = GetAllPatientsForDoctor(doctorId);
                ViewBag.Patients = patients;

                // Get statistics
                var patientStats = GetPatientStatistics(doctorId);
                ViewBag.PatientStats = patientStats;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading patients: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult DownloadPrescription(int id)
        {
            if (!IsDoctorLoggedIn())
                return RedirectToAction("Login", "Account");

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var cmd = new SqlCommand("SELECT PrescriptionFile FROM Prescriptions WHERE PrescriptionId=@Id AND ISNULL(IsDeleted,0)=0", connection);
            cmd.Parameters.AddWithValue("@Id", id);

            var fileName = cmd.ExecuteScalar() as string;
            if (string.IsNullOrEmpty(fileName))
            {
                TempData["ErrorMessage"] = "File not found";
                return RedirectToAction("Dashboard");
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "prescriptions", fileName);

            if (!System.IO.File.Exists(path))
            {
                TempData["ErrorMessage"] = "File missing from server";
                return RedirectToAction("Dashboard");
            }

            // content-type basic
            var ext = Path.GetExtension(fileName).ToLower();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return PhysicalFile(path, contentType, fileName);
        }

        private List<dynamic> GetAllPatientsForDoctor(int doctorId)
        {
            var patients = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT DISTINCT 
                u.UserId,
                u.FullName,
                u.DateOfBirth,
                u.Gender,
                u.PhoneNumber,
                p.BloodGroup,
                p.Height,
                p.Weight,
                p.MaritalStatus,
                DATEDIFF(YEAR, u.DateOfBirth, GETDATE()) AS Age,
                MAX(a.AppointmentDate) AS LastVisit,
                COUNT(a.AppointmentId) AS TotalVisits,
                MIN(a.AppointmentDate) AS FirstVisit,
                SUM(CASE WHEN a.Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedVisits
            FROM Appointments a
            INNER JOIN Users u ON a.PatientId = u.UserId
            LEFT JOIN Patients p ON u.UserId = p.PatientId
            WHERE a.DoctorId = @DoctorId
            AND u.UserType = 'Patient'
            GROUP BY u.UserId, u.FullName, u.DateOfBirth, u.Gender, u.PhoneNumber, 
                     p.BloodGroup, p.Height, p.Weight, p.MaritalStatus
            ORDER BY LastVisit DESC, TotalVisits DESC",
                    connection);
                cmd.Parameters.AddWithValue("@DoctorId", doctorId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        decimal? height = reader.IsDBNull(6) ? null : reader.GetDecimal(6);
                        decimal? weight = reader.IsDBNull(7) ? null : reader.GetDecimal(7);
                        string bmi = "N/A";

                        if (height.HasValue && weight.HasValue && height.Value > 0)
                        {
                            var heightInM = height.Value / 100;
                            bmi = Math.Round(weight.Value / (heightInM * heightInM), 2).ToString();
                        }

                        patients.Add(new
                        {
                            UserId = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            DateOfBirth = reader.IsDBNull(2) ? "Not set" : reader.GetDateTime(2).ToString("dd-MMM-yyyy"),
                            Gender = reader.IsDBNull(3) ? "Not set" : reader.GetString(3),
                            PhoneNumber = reader.IsDBNull(4) ? "Not set" : reader.GetString(4),
                            BloodGroup = reader.IsDBNull(5) ? "Not set" : reader.GetString(5),
                            Height = height.HasValue ? $"{height.Value} cm" : "Not set",
                            Weight = weight.HasValue ? $"{weight.Value} kg" : "Not set",
                            MaritalStatus = reader.IsDBNull(8) ? "Not set" : reader.GetString(8),
                            Age = reader.IsDBNull(9) ? "N/A" : reader.GetInt32(9).ToString(),
                            LastVisit = reader.GetDateTime(10).ToString("dd-MMM-yyyy"),
                            TotalVisits = reader.GetInt32(11),
                            FirstVisit = reader.GetDateTime(12).ToString("dd-MMM-yyyy"),
                            CompletedVisits = reader.GetInt32(13),
                            BMI = bmi
                        });
                    }
                }
            }
            return patients;
        }

        // Helper method: Get patient statistics
        private dynamic GetPatientStatistics(int doctorId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT 
                COUNT(DISTINCT a.PatientId) as TotalPatients,
                COUNT(a.AppointmentId) as TotalAppointments,
                AVG(CAST(DATEDIFF(YEAR, u.DateOfBirth, GETDATE()) AS FLOAT)) as AvgAge,
                COUNT(DISTINCT p.BloodGroup) as UniqueBloodGroups,
                SUM(CASE WHEN a.Status = 'Completed' THEN 1 ELSE 0 END) as CompletedAppointments
            FROM Appointments a
            INNER JOIN Users u ON a.PatientId = u.UserId
            LEFT JOIN Patients p ON u.UserId = p.PatientId
            WHERE a.DoctorId = @DoctorId
            AND u.UserType = 'Patient'
            GROUP BY a.DoctorId",
                        connection);
                cmd.Parameters.AddWithValue("@DoctorId", doctorId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            TotalPatients = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                            TotalAppointments = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                            AvgAge = reader.IsDBNull(2) ? 0 : Math.Round(reader.GetDouble(2), 1),
                            UniqueBloodGroups = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                            CompletedAppointments = reader.IsDBNull(4) ? 0 : reader.GetInt32(4)
                        };
                    }
                }
            }
            return null;
        }

        // POST: /Doctor/UpdateProfilePicture
        [HttpPost]
        public JsonResult UpdateProfilePicture(IFormFile profilePicture)
        {
            if (!IsDoctorLoggedIn())
                return Json(new { success = false, message = "Not logged in" });

            var userIdBytes = HttpContext.Session.Get("UserId");
            if (userIdBytes == null || !int.TryParse(Encoding.UTF8.GetString(userIdBytes), out int userId))
                return Json(new { success = false, message = "Invalid session" });

            if (profilePicture != null && profilePicture.Length > 0)
            {
                // Save the file
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile-pictures");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(profilePicture.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    profilePicture.CopyTo(stream);
                }

                // Update database
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "UPDATE Users SET ProfilePicture = @ProfilePicture, UpdatedAt = GETDATE() WHERE UserId = @UserId",
                        connection);
                    cmd.Parameters.AddWithValue("@ProfilePicture", uniqueFileName);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }

                // ✅ Session-এ আপডেট করুন
                HttpContext.Session.Set("ProfilePicture", Encoding.UTF8.GetBytes(uniqueFileName));

                return Json(new { success = true, fileName = uniqueFileName });
            }

            return Json(new { success = false, message = "No file uploaded" });
        }

        // POST: /Doctor/RemoveProfilePicture
        [HttpPost]
        public JsonResult RemoveProfilePicture()
        {
            if (!IsDoctorLoggedIn())
                return Json(new { success = false, message = "Not logged in" });

            var userIdBytes = HttpContext.Session.Get("UserId");
            if (userIdBytes == null || !int.TryParse(Encoding.UTF8.GetString(userIdBytes), out int userId))
                return Json(new { success = false, message = "Invalid session" });

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "UPDATE Users SET ProfilePicture = 'default.webp', UpdatedAt = GETDATE() WHERE UserId = @UserId",
                        connection);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }

                // ✅ Session-এ আপডেট করুন
                HttpContext.Session.Set("ProfilePicture", Encoding.UTF8.GetBytes("default.webp"));

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== HELPER METHODS ====================

        private dynamic GetDoctorDetails(int doctorId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT 
                        u.UserId,
                        u.FullName,
                        u.Email,
                        u.DateOfBirth,
                        u.Gender,
                        u.PhoneNumber,
                        u.Address,
                        u.ProfilePicture,
                        u.CreatedAt,
                        d.Specialization,
                        d.Qualification,
                        d.LicenseNumber,
                        d.ConsultationFee,
                        d.AvailableDays,
                        d.AvailableTime,
                        d.ExperienceYears,
                        d.Department
                    FROM Users u
                    INNER JOIN Doctors d ON u.UserId = d.DoctorId
                    WHERE u.UserId = @UserId AND u.UserType = 'Doctor'",
                    connection);
                cmd.Parameters.AddWithValue("@UserId", doctorId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            UserId = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            Email = reader.GetString(2),
                            DateOfBirth = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                            Gender = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            PhoneNumber = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            Address = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            ProfilePicture = reader.IsDBNull(7) ? "default.webp" : reader.GetString(7),
                            CreatedAt = reader.GetDateTime(8),
                            Specialization = reader.GetString(9),
                            Qualification = reader.GetString(10),
                            LicenseNumber = reader.GetString(11),
                            ConsultationFee = reader.GetDecimal(12),
                            AvailableDays = reader.GetString(13),
                            AvailableTime = reader.GetString(14),
                            ExperienceYears = reader.IsDBNull(15) ? 0 : reader.GetInt32(15),
                            Department = reader.IsDBNull(16) ? "" : reader.GetString(16)
                        };
                    }
                }
            }
            return null;
        }

        private List<dynamic> GetTodaysAppointments(int doctorId)
        {
            var appointments = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT 
                        a.AppointmentId,
                        a.PatientId,
                        a.AppointmentDate,
                        CONVERT(VARCHAR(5), a.AppointmentTime, 108) as AppointmentTime,
                        a.Status,
                        a.Reason,
                        u.FullName AS PatientName,
                        p.BloodGroup
                    FROM Appointments a
                    INNER JOIN Users u ON a.PatientId = u.UserId
                    LEFT JOIN Patients p ON a.PatientId = p.PatientId
                    WHERE a.DoctorId = @DoctorId 
                    AND a.AppointmentDate = CAST(GETDATE() AS DATE)
                    AND a.Status IN ('Pending', 'Approved')
                    ORDER BY a.AppointmentTime",
                    connection);
                cmd.Parameters.AddWithValue("@DoctorId", doctorId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new
                        {
                            AppointmentId = reader["AppointmentId"],
                            PatientId = reader["PatientId"],
                            AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]).ToString("yyyy-MM-dd"),
                            AppointmentTime = reader["AppointmentTime"].ToString(),
                            Status = reader["Status"].ToString(),
                            Reason = reader["Reason"].ToString(),
                            PatientName = reader["PatientName"].ToString(),
                            BloodGroup = reader.IsDBNull(7) ? "Not set" : reader["BloodGroup"].ToString()
                        });
                    }
                }
            }
            return appointments;
        }

        private List<dynamic> GetUpcomingAppointments(int doctorId)
        {
            var appointments = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT 
                        a.AppointmentId,
                        a.PatientId,
                        a.AppointmentDate,
                        CONVERT(VARCHAR(5), a.AppointmentTime, 108) as AppointmentTime,
                        a.Status,
                        a.Reason,
                        u.FullName AS PatientName,
                        p.BloodGroup
                    FROM Appointments a
                    INNER JOIN Users u ON a.PatientId = u.UserId
                    LEFT JOIN Patients p ON a.PatientId = p.PatientId
                    WHERE a.DoctorId = @DoctorId 
                    AND a.AppointmentDate > CAST(GETDATE() AS DATE)
                    AND a.Status IN ('Pending', 'Approved')
                    ORDER BY a.AppointmentDate, a.AppointmentTime",
                    connection);
                cmd.Parameters.AddWithValue("@DoctorId", doctorId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new
                        {
                            AppointmentId = reader["AppointmentId"],
                            PatientId = reader["PatientId"],
                            AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]).ToString("dd-MMM-yy"),
                            AppointmentTime = reader["AppointmentTime"].ToString(),
                            Status = reader["Status"].ToString(),
                            Reason = reader["Reason"].ToString(),
                            PatientName = reader["PatientName"].ToString(),
                            BloodGroup = reader.IsDBNull(7) ? "Not set" : reader["BloodGroup"].ToString()
                        });
                    }
                }
            }
            return appointments;
        }

        private dynamic GetDoctorStatistics(int doctorId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT 
                        COUNT(CASE WHEN Status = 'Pending' THEN 1 END) AS PendingAppointments,
                        COUNT(CASE WHEN Status = 'Approved' THEN 1 END) AS ApprovedAppointments,
                        COUNT(CASE WHEN Status = 'Completed' THEN 1 END) AS CompletedAppointments,
                        COUNT(DISTINCT PatientId) AS TotalPatients
                    FROM Appointments
                    WHERE DoctorId = @DoctorId",
                    connection);
                cmd.Parameters.AddWithValue("@DoctorId", doctorId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            PendingAppointments = reader.GetInt32(0),
                            ApprovedAppointments = reader.GetInt32(1),
                            CompletedAppointments = reader.GetInt32(2),
                            TotalPatients = reader.GetInt32(3)
                        };
                    }
                }
            }
            return null;
        }

        private List<dynamic> GetRecentPatients(int doctorId)
        {
            var patients = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT DISTINCT
                        u.UserId,
                        u.FullName,
                        p.BloodGroup,
                        MAX(a.AppointmentDate) AS LastVisit,
                        COUNT(a.AppointmentId) AS TotalVisits
                    FROM Appointments a
                    INNER JOIN Users u ON a.PatientId = u.UserId
                    LEFT JOIN Patients p ON a.PatientId = p.PatientId
                    WHERE a.DoctorId = @DoctorId
                    GROUP BY u.UserId, u.FullName, p.BloodGroup
                    ORDER BY LastVisit DESC",
                    connection);
                cmd.Parameters.AddWithValue("@DoctorId", doctorId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        patients.Add(new
                        {
                            UserId = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            BloodGroup = reader.IsDBNull(2) ? "Not set" : reader["BloodGroup"].ToString(),
                            LastVisit = reader.GetDateTime(3).ToString("dd-MMM-yy"),
                            TotalVisits = reader.GetInt32(4)
                        });
                    }
                }
            }
            return patients;
        }

        private List<dynamic> GetDoctorAppointments(int doctorId)
        {
            var appointments = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT 
                        a.AppointmentId,
                        a.PatientId,
                        a.AppointmentDate,
                        CONVERT(VARCHAR(5), a.AppointmentTime, 108) as AppointmentTime,
                        a.Status,
                        a.Reason,
                        a.Symptoms,
                        u.FullName AS PatientName,
                        p.BloodGroup,
                        p.Height,
                        p.Weight
                    FROM Appointments a
                    INNER JOIN Users u ON a.PatientId = u.UserId
                    LEFT JOIN Patients p ON a.PatientId = p.PatientId
                    WHERE a.DoctorId = @DoctorId
                    ORDER BY a.AppointmentDate DESC, a.AppointmentTime DESC",
                    connection);
                cmd.Parameters.AddWithValue("@DoctorId", doctorId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new
                        {
                            AppointmentId = reader["AppointmentId"],
                            PatientId = reader["PatientId"],
                            AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]).ToString("dd-MMM-yyyy"),
                            AppointmentTime = reader["AppointmentTime"].ToString(),
                            Status = reader["Status"].ToString(),
                            Reason = reader["Reason"].ToString(),
                            Symptoms = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            PatientName = reader["PatientName"].ToString(),
                            BloodGroup = reader.IsDBNull(8) ? "Not set" : reader["BloodGroup"].ToString(),
                            Height = reader.IsDBNull(9) ? "N/A" : $"{reader.GetDecimal(9)} cm",
                            Weight = reader.IsDBNull(10) ? "N/A" : $"{reader.GetDecimal(10)} kg"
                        });
                    }
                }
            }
            return appointments;
        }

        private List<dynamic> GetPatientAppointmentsWithDoctor(int patientId, int doctorId)
        {
            var appointments = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT 
                        AppointmentId,
                        AppointmentDate,
                        CONVERT(VARCHAR(5), AppointmentTime, 108) as AppointmentTime,
                        Status,
                        Reason,
                        Symptoms
                    FROM Appointments
                    WHERE PatientId = @PatientId AND DoctorId = @DoctorId
                    ORDER BY AppointmentDate DESC, AppointmentTime DESC",
                    connection);
                cmd.Parameters.AddWithValue("@PatientId", patientId);
                cmd.Parameters.AddWithValue("@DoctorId", doctorId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new
                        {
                            AppointmentId = reader["AppointmentId"],
                            AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]).ToString("dd-MMM-yyyy"),
                            AppointmentTime = reader["AppointmentTime"].ToString(),
                            Status = reader["Status"].ToString(),
                            Reason = reader["Reason"].ToString(),
                            Symptoms = reader.IsDBNull(5) ? "" : reader.GetString(5)
                        });
                    }
                }
            }
            return appointments;
        }

        private List<dynamic> GetPatientMedicalHistory(int patientId)
        {
            var history = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT 
                d.DiseaseName,
                pdh.DiagnosedDate,
                pdh.Status,
                pdh.Notes
            FROM PatientDiseaseHistory pdh
            INNER JOIN Diseases d ON pdh.DiseaseId = d.DiseaseId
            WHERE pdh.PatientId = @PatientId
            ORDER BY pdh.DiagnosedDate DESC",
                    connection);
                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        history.Add(new
                        {
                            DiseaseName = reader["DiseaseName"].ToString(),
                            DiagnosedDate = Convert.ToDateTime(reader["DiagnosedDate"]).ToString("dd-MMM-yyyy"),
                            Status = reader["Status"].ToString(),
                            Notes = reader.IsDBNull(3) ? "" : reader.GetString(3)
                        });
                    }
                }
            }
            return history;
        }

        // ✅ FIXED: GetPatientTestReports - সব Doctor সব Test Reports দেখতে পারবে
        private List<dynamic> GetPatientTestReports(int patientId)
        {
            var reports = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // ✅ সব Doctor সব Test Reports দেখতে পারবে - কোনো restriction নেই
                var cmd = new SqlCommand(@"
            SELECT 
                tr.ReportId,
                tr.ReportName,
                tr.ReportDate,
                tr.ReportFile,
                ISNULL(tr.Notes, '') as Notes,
                FORMAT(tr.UploadedAt, 'dd-MMM-yyyy HH:mm') as UploadedAt,
                ISNULL(u.FullName, 'System') as UploadedByName,
                tr.PatientId
            FROM TestReports tr
            LEFT JOIN Users u ON tr.UploadedBy = u.UserId
            WHERE tr.PatientId = @PatientId
            ORDER BY tr.ReportDate DESC, tr.UploadedAt DESC",
                    connection);

                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reports.Add(new
                        {
                            ReportId = reader.GetInt32(0),
                            ReportName = reader.GetString(1),
                            ReportDate = Convert.ToDateTime(reader["ReportDate"]).ToString("dd-MMM-yyyy"),
                            ReportFile = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Notes = reader.GetString(4),
                            UploadedAt = reader.GetString(5),
                            UploadedByName = reader.GetString(6),
                            PatientId = reader.GetInt32(7),
                            FileExists = !reader.IsDBNull(3) && !string.IsNullOrEmpty(reader["ReportFile"]?.ToString())
                        });
                    }
                }
            }
            return reports;
        }

        // ✅ FIXED: GetPatientPrescriptions - সব Doctor সব Prescriptions দেখতে পারবে
        private List<dynamic> GetPatientPrescriptions(int patientId)
        {
            var prescriptions = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // ✅ সব Doctor সব Prescriptions দেখতে পারবে - কোনো restriction নেই
                var cmd = new SqlCommand(@"
            SELECT 
                p.PrescriptionId, 
                FORMAT(p.PrescriptionDate, 'dd-MMM-yyyy') as PrescriptionDate,
                p.PrescriptionFile, 
                ISNULL(p.FileSize, 0) as FileSize,
                ISNULL(p.FileType, '') as FileType,
                ISNULL(p.Notes, '') as Notes,
                FORMAT(p.UploadedAt, 'dd-MMM-yyyy HH:mm') as UploadedAt,
                u.FullName as PrescribedByName,
                u.UserType as PrescribedByUserType,
                p.PatientId
            FROM Prescriptions p
            INNER JOIN Users u ON p.PrescribedBy = u.UserId
            WHERE p.PatientId = @PatientId
            AND ISNULL(p.IsDeleted,0) = 0
            ORDER BY p.PrescriptionDate DESC, p.UploadedAt DESC",
                    connection);

                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        prescriptions.Add(new
                        {
                            PrescriptionId = reader.GetInt32(0),
                            PrescriptionDate = reader.GetString(1),
                            PrescriptionFile = reader.GetString(2),
                            FileSize = reader.GetInt32(3),
                            FileType = reader.GetString(4),
                            Notes = reader.GetString(5),
                            UploadedAt = reader.GetString(6),
                            PrescribedByName = reader.GetString(7),
                            PrescribedByUserType = reader.GetString(8),
                            PatientId = reader.GetInt32(9)
                        });
                    }
                }
            }
            return prescriptions;
        }

        private dynamic GetTestReportDetails(int reportId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
            SELECT 
                r.ReportId,
                r.PatientId,
                r.ReportName,
                r.ReportDate,
                r.ReportFile,
                r.Notes,
                u.FullName AS PatientName,
                ISNULL(up.FullName, 'System') AS UploadedByName
            FROM TestReports r
            INNER JOIN Users u ON r.PatientId = u.UserId
            LEFT JOIN Users up ON r.UploadedBy = up.UserId
            WHERE r.ReportId = @ReportId",
                    connection);

                cmd.Parameters.AddWithValue("@ReportId", reportId);

                try
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new
                            {
                                ReportId = reader.GetInt32(0),
                                PatientId = reader.GetInt32(1),
                                ReportName = reader.GetString(2),
                                ReportDate = reader.GetDateTime(3),
                                ReportFile = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Notes = reader.IsDBNull(5) ? null : reader.GetString(5),
                                PatientName = reader.GetString(6),
                                UploadedByName = reader.GetString(7),
                                FileExists = !reader.IsDBNull(4) && !string.IsNullOrEmpty(reader.GetString(4))
                            };
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"SQL Error: {ex.Message}");
                    throw;
                }
            }
            return null;
        }

        private dynamic GetPrescriptionDetails(int prescriptionId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT 
                p.PrescriptionId,
                p.PatientId,
                p.PrescriptionDate,
                p.PrescriptionFile,
                p.Notes,
                u.FullName AS PatientName,
                dr.FullName AS PrescribedByName,
                p.UploadedAt,
                ISNULL(p.IsUploadedByNurse,0) AS IsUploadedByNurse
            FROM Prescriptions p
            INNER JOIN Users u ON p.PatientId = u.UserId
            LEFT JOIN Users dr ON p.PrescribedBy = dr.UserId
            WHERE p.PrescriptionId = @PrescriptionId AND ISNULL(p.IsDeleted,0) = 0",
                    connection);
                cmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            PrescriptionId = reader.GetInt32(0),
                            PatientId = reader.GetInt32(1),
                            PrescriptionDate = reader.GetDateTime(2),
                            PrescriptionFile = reader.GetString(3),
                            Notes = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            PatientName = reader.GetString(5),
                            PrescribedByName = reader.IsDBNull(6) ? "Unknown" : reader.GetString(6),
                            UploadedAt = reader.GetDateTime(7),
                            IsUploadedByNurse = reader.GetBoolean(8)
                        };
                    }
                }
            }
            return null;
        }

        public IActionResult DownloadReport(int id)
        {
            if (!IsDoctorLoggedIn())
                return RedirectToAction("Login", "Account");

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var cmd = new SqlCommand("SELECT ReportFile FROM TestReports WHERE ReportId=@Id", connection);
            cmd.Parameters.AddWithValue("@Id", id);

            var fileName = cmd.ExecuteScalar() as string;
            if (string.IsNullOrEmpty(fileName))
            {
                TempData["ErrorMessage"] = "File not found";
                return RedirectToAction("Dashboard");
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reports", fileName);

            if (!System.IO.File.Exists(path))
            {
                TempData["ErrorMessage"] = "File missing from server";
                return RedirectToAction("Dashboard");
            }

            return PhysicalFile(path, "application/pdf", fileName);
        }


        private bool IsPatientOfDoctor(int patientId, int doctorId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Appointments WHERE PatientId = @PatientId AND DoctorId = @DoctorId",
                    connection);
                cmd.Parameters.AddWithValue("@PatientId", patientId);
                cmd.Parameters.AddWithValue("@DoctorId", doctorId);

                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        private DoctorProfileViewModel GetDoctorProfile(int doctorId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT 
                u.UserId,
                u.FullName,
                u.Email,
                u.DateOfBirth,
                u.Gender,
                u.PhoneNumber,
                u.Address,
                u.ProfilePicture,
                d.Specialization,
                d.Qualification,
                d.LicenseNumber,
                d.ConsultationFee,
                d.AvailableDays,
                d.AvailableTime,
                d.ExperienceYears,
                d.Department
            FROM Users u
            INNER JOIN Doctors d ON u.UserId = d.DoctorId
            WHERE u.UserId = @UserId AND u.IsActive = 1",
                    connection);
                cmd.Parameters.AddWithValue("@UserId", doctorId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new DoctorProfileViewModel
                        {
                            UserId = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            Email = reader.GetString(2),
                            DateOfBirth = reader.IsDBNull(3) ? null : (DateTime?)reader.GetDateTime(3),
                            Gender = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            PhoneNumber = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            Address = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            ProfilePicture = reader.IsDBNull(7) ? "default.webp" : reader.GetString(7),
                            Specialization = reader.GetString(8),
                            Qualification = reader.GetString(9),
                            LicenseNumber = reader.GetString(10),
                            ConsultationFee = reader.GetDecimal(11),
                            AvailableDays = reader.GetString(12),
                            AvailableTime = reader.GetString(13),
                            ExperienceYears = reader.IsDBNull(14) ? 0 : reader.GetInt32(14),
                            Department = reader.IsDBNull(15) ? "" : reader.GetString(15)
                        };
                    }
                }
            }
            return null;
        }

        private void UpdateDoctorProfile(int doctorId, DoctorProfileViewModel model)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Update Users table
                var userCmd = new SqlCommand(@"
            UPDATE Users 
            SET FullName = @FullName,
                DateOfBirth = @DateOfBirth,
                Gender = @Gender,
                PhoneNumber = @PhoneNumber,
                Address = @Address,
                UpdatedAt = GETDATE()
            WHERE UserId = @UserId",
                    connection);

                userCmd.Parameters.AddWithValue("@FullName", model.FullName);
                userCmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth ?? (object)DBNull.Value);
                userCmd.Parameters.AddWithValue("@Gender", model.Gender ?? (object)DBNull.Value);
                userCmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber ?? (object)DBNull.Value);
                userCmd.Parameters.AddWithValue("@Address", model.Address ?? (object)DBNull.Value);
                userCmd.Parameters.AddWithValue("@UserId", doctorId);
                userCmd.ExecuteNonQuery();

                // ✅ Update Doctors table with DEBUG information
                var doctorCmd = new SqlCommand(@"
            UPDATE Doctors 
            SET Specialization = @Specialization,
                Qualification = @Qualification,
                LicenseNumber = @LicenseNumber,
                ConsultationFee = @ConsultationFee,
                AvailableDays = @AvailableDays,
                AvailableTime = @AvailableTime,
                ExperienceYears = @ExperienceYears,
                Department = @Department
            WHERE DoctorId = @DoctorId",
                    connection);

                doctorCmd.Parameters.AddWithValue("@Specialization", model.Specialization);
                doctorCmd.Parameters.AddWithValue("@Qualification", model.Qualification);
                doctorCmd.Parameters.AddWithValue("@LicenseNumber", model.LicenseNumber);
                doctorCmd.Parameters.AddWithValue("@ConsultationFee", model.ConsultationFee);
                doctorCmd.Parameters.AddWithValue("@AvailableDays", model.AvailableDays);
                doctorCmd.Parameters.AddWithValue("@AvailableTime", model.AvailableTime);
                doctorCmd.Parameters.AddWithValue("@ExperienceYears", model.ExperienceYears);

                if (!string.IsNullOrEmpty(model.Department))
                {
                    doctorCmd.Parameters.AddWithValue("@Department", model.Department.Trim());
                }
                else
                {
                    doctorCmd.Parameters.AddWithValue("@Department", DBNull.Value);
                }

                doctorCmd.Parameters.AddWithValue("@DoctorId", doctorId);

                int rowsAffected = doctorCmd.ExecuteNonQuery();
                Console.WriteLine($"Rows affected: {rowsAffected}");
            }
        }
    }
}