using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using SunriseClinic.Models;

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
        private bool IsDoctorLoggedIn()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");
            return !string.IsNullOrEmpty(userId) && userType == "Doctor";
        }

        // GET: /Doctor/Dashboard
        public IActionResult Dashboard()
        {
            if (!IsDoctorLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            try
            {
                // Get doctor details
                var doctor = GetDoctorDetails(userId);
                ViewBag.Doctor = doctor;

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

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            var appointments = GetDoctorAppointments(userId);
            ViewBag.Appointments = appointments;

            return View();
        }

        // GET: /Doctor/PatientDetails/{patientId}
        public IActionResult PatientDetails(int patientId)
        {
            if (!IsDoctorLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                // Get patient basic info (without contact details)
                var patient = GetPatientInfoForDoctor(patientId);
                if (patient == null)
                {
                    TempData["ErrorMessage"] = "Patient not found";
                    return RedirectToAction("Dashboard");
                }

                ViewBag.Patient = patient;

                // Get patient appointments with this doctor
                var appointments = GetPatientAppointmentsWithDoctor(patientId,
                    int.Parse(HttpContext.Session.GetString("UserId")));
                ViewBag.Appointments = appointments;

                // Get patient medical history (without contact details)
                var medicalHistory = GetPatientMedicalHistory(patientId);
                ViewBag.MedicalHistory = medicalHistory;

                // Get patient test reports
                var testReports = GetPatientTestReports(patientId);
                ViewBag.TestReports = testReports;

                // Get patient prescriptions
                var prescriptions = GetPatientPrescriptions(patientId);
                ViewBag.Prescriptions = prescriptions;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading patient details: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        // GET: /Doctor/ViewReport/{reportId}
        public IActionResult ViewReport(int reportId)
        {
            if (!IsDoctorLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            try
            {
                var report = GetTestReportDetails(reportId);
                if (report == null)
                {
                    TempData["ErrorMessage"] = "Report not found";
                    return RedirectToAction("Dashboard");
                }

                // Check if report belongs to doctor's patient
                if (!IsPatientOfDoctor(report.PatientId, userId))
                {
                    TempData["ErrorMessage"] = "You don't have permission to view this report";
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
        public IActionResult ViewPrescription(int prescriptionId)
        {
            if (!IsDoctorLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            try
            {
                var prescription = GetPrescriptionDetails(prescriptionId);
                if (prescription == null)
                {
                    TempData["ErrorMessage"] = "Prescription not found";
                    return RedirectToAction("Dashboard");
                }

                // Check if prescription is for doctor's patient
                if (!IsPatientOfDoctor(prescription.PatientId, userId))
                {
                    TempData["ErrorMessage"] = "You don't have permission to view this prescription";
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

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            var doctor = GetDoctorProfile(userId);
            if (doctor == null)
                return RedirectToAction("Login", "Account");

            ViewBag.Doctor = doctor;
            return View();
        }

        // POST: /Doctor/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProfile(DoctorProfileViewModel model)
        {
            if (!IsDoctorLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                try
                {
                    UpdateDoctorProfile(userId, model);

                    // Update session
                    HttpContext.Session.SetString("UserName", model.FullName);

                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction("Profile");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating profile: {ex.Message}");
                }
            }

            var doctor = GetDoctorProfile(userId);
            ViewBag.Doctor = doctor;
            return View("Profile", model);
        }

        // POST: /Doctor/UpdateProfilePicture
        [HttpPost]
        public JsonResult UpdateProfilePicture(IFormFile profilePicture)
        {
            if (!IsDoctorLoggedIn())
                return Json(new { success = false, message = "Not logged in" });

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
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

            var userId = int.Parse(HttpContext.Session.GetString("UserId"));

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "UPDATE Users SET ProfilePicture = 'default.jpg', UpdatedAt = GETDATE() WHERE UserId = @UserId",
                        connection);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }

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
                    WHERE u.UserId = @UserId AND u.UserType = 'Doctor' AND u.IsActive = 1",
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
                            ProfilePicture = reader.IsDBNull(7) ? "default.jpg" : reader.GetString(7),
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
                a.PatientId, -- ✅ এই লাইন যোগ করুন
                a.AppointmentDate,
                CONVERT(VARCHAR(5), a.AppointmentTime, 108) as AppointmentTime,
                a.Status,
                a.Reason,
                a.Symptoms,
                u.FullName AS PatientName,
                p.BloodGroup,
                CASE 
                    WHEN p.Height IS NOT NULL AND p.Weight IS NOT NULL 
                    THEN CAST(ROUND(p.Weight / ((p.Height/100) * (p.Height/100)), 2) AS NVARCHAR(10))
                    ELSE 'N/A'
                END as BMI
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
                            PatientId = reader["PatientId"], // ✅ এই লাইন যোগ করুন
                            AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]).ToString("yyyy-MM-dd"),
                            AppointmentTime = reader["AppointmentTime"].ToString(),
                            Status = reader["Status"].ToString(),
                            Reason = reader["Reason"].ToString(),
                            Symptoms = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            PatientName = reader["PatientName"].ToString(),
                            BloodGroup = reader.IsDBNull(7) ? "Not set" : reader["BloodGroup"].ToString(),
                            BMI = reader["BMI"].ToString()
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
                a.PatientId, -- ✅ এই লাইন যোগ করুন
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
            ORDER BY a.AppointmentDate, a.AppointmentTime
            OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY",
                    connection);
                cmd.Parameters.AddWithValue("@DoctorId", doctorId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new
                        {
                            AppointmentId = reader["AppointmentId"],
                            PatientId = reader["PatientId"], // ✅ এই লাইন যোগ করুন
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
                        (SELECT COUNT(*) FROM Appointments WHERE DoctorId = @DoctorId AND Status = 'Pending') AS PendingAppointments,
                        (SELECT COUNT(*) FROM Appointments WHERE DoctorId = @DoctorId AND Status = 'Approved') AS ApprovedAppointments,
                        (SELECT COUNT(*) FROM Appointments WHERE DoctorId = @DoctorId AND Status = 'Completed') AS CompletedAppointments,
                        (SELECT COUNT(DISTINCT PatientId) FROM Appointments WHERE DoctorId = @DoctorId) AS TotalPatients",
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
                    SELECT DISTINCT TOP 5
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
                a.PatientId, -- ✅ এই লাইন যোগ করুন
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
                            PatientId = reader["PatientId"], // ✅ এই লাইন যোগ করুন
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
                        p.BloodGroup,
                        p.Height,
                        p.Weight,
                        p.MaritalStatus,
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
                        decimal? height = reader.IsDBNull(5) ? null : reader.GetDecimal(5);
                        decimal? weight = reader.IsDBNull(6) ? null : reader.GetDecimal(6);
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
                            BloodGroup = reader.IsDBNull(4) ? "Not set" : reader.GetString(4),
                            Height = height.HasValue ? $"{height.Value} cm" : "Not set",
                            Weight = weight.HasValue ? $"{weight.Value} kg" : "Not set",
                            MaritalStatus = reader.IsDBNull(7) ? "Not set" : reader.GetString(7),
                            Age = reader.IsDBNull(8) ? "N/A" : reader.GetInt32(8).ToString(),
                            BMI = bmi
                        };
                    }
                }
            }
            return null;
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

        private List<dynamic> GetPatientTestReports(int patientId)
        {
            var reports = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT 
                        ReportId,
                        ReportName,
                        ReportDate,
                        Notes
                    FROM TestReports
                    WHERE PatientId = @PatientId
                    ORDER BY ReportDate DESC",
                    connection);
                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reports.Add(new
                        {
                            ReportId = reader["ReportId"],
                            ReportName = reader["ReportName"].ToString(),
                            ReportDate = Convert.ToDateTime(reader["ReportDate"]).ToString("dd-MMM-yyyy"),
                            Notes = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            FileExists = !reader.IsDBNull(reader.GetOrdinal("ReportFile"))
                        });
                    }
                }
            }
            return reports;
        }

        private List<dynamic> GetPatientPrescriptions(int patientId)
        {
            var prescriptions = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT 
                        PrescriptionId,
                        PrescriptionDate,
                        Notes,
                        PrescribedByName
                    FROM Prescriptions p
                    INNER JOIN Users u ON p.PrescribedBy = u.UserId
                    WHERE p.PatientId = @PatientId AND p.IsDeleted = 0
                    ORDER BY PrescriptionDate DESC",
                    connection);
                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        prescriptions.Add(new
                        {
                            PrescriptionId = reader["PrescriptionId"],
                            PrescriptionDate = Convert.ToDateTime(reader["PrescriptionDate"]).ToString("dd-MMM-yyyy"),
                            Notes = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            PrescribedByName = reader["PrescribedByName"].ToString()
                        });
                    }
                }
            }
            return prescriptions;
        }

        private TestReport GetTestReportDetails(int reportId)
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
                        u.FullName AS PatientName
                    FROM TestReports r
                    INNER JOIN Users u ON r.PatientId = u.UserId
                    WHERE r.ReportId = @ReportId",
                    connection);
                cmd.Parameters.AddWithValue("@ReportId", reportId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new TestReport
                        {
                            ReportId = reader.GetInt32(0),
                            PatientId = reader.GetInt32(1),
                            ReportName = reader.GetString(2),
                            ReportDate = reader.GetDateTime(3),
                            ReportFile = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Notes = reader.IsDBNull(5) ? null : reader.GetString(5),
                            PatientName = reader.GetString(6)
                        };
                    }
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
                        dr.FullName AS PrescribedByName
                    FROM Prescriptions p
                    INNER JOIN Users u ON p.PatientId = u.UserId
                    INNER JOIN Users dr ON p.PrescribedBy = dr.UserId
                    WHERE p.PrescriptionId = @PrescriptionId AND p.IsDeleted = 0",
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
                            PrescribedByName = reader.GetString(6)
                        };
                    }
                }
            }
            return null;
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

        private dynamic GetDoctorProfile(int doctorId)
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
                    WHERE u.UserId = @UserId",
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
                            DateOfBirth = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                            Gender = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            PhoneNumber = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            Address = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            ProfilePicture = reader.IsDBNull(7) ? "default.jpg" : reader.GetString(7),
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

                // Update Doctors table
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
                doctorCmd.Parameters.AddWithValue("@Department", model.Department ?? (object)DBNull.Value);
                doctorCmd.Parameters.AddWithValue("@DoctorId", doctorId);
                doctorCmd.ExecuteNonQuery();
            }
        }
    }
}