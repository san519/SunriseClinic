using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SunriseClinic.Models;
using System.Data;
using System.Security.Claims;

namespace SunriseClinic.Controllers
{
    public class PatientController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public PatientController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        // Check if user is logged in and is a patient
        private bool IsPatientLoggedIn()
        {
            // ✅ শুধু Cookie-based auth চেক করুন
            return User?.Identity?.IsAuthenticated == true && User.IsInRole("Patient");
        }

        // Get patient ID from claims
        private int? GetLoggedPatientId()
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


        // GET: /Patient/Dashboard
        public IActionResult Dashboard()
        {
            if (!IsPatientLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            // Check for registration success message
            var registrationSuccess = TempData["RegistrationSuccess"] as bool?;
            var patientId = TempData["PatientId"] as string;

            if (registrationSuccess == true && !string.IsNullOrEmpty(patientId))
            {
                ViewBag.RegistrationSuccess = true;
                ViewBag.PatientId = patientId;
            }

            // Get patient details
            var patient = GetPatientDetails(userId);
            ViewBag.Patient = patient;

            // ✅ Session-এ Profile Picture সেট করুন
            if (patient != null && !string.IsNullOrEmpty(patient.ProfilePicture))
            {
                HttpContext.Session.Set("ProfilePicture", System.Text.Encoding.UTF8.GetBytes(patient.ProfilePicture));
            }

            ViewBag.PatientProfilePicture = GetPatientProfilePicture(userId);

            // Get upcoming appointments
            var appointments = GetUpcomingAppointments(userId);
            ViewBag.Appointments = appointments;

            // Get recent test reports
            var reports = GetRecentTestReports(userId);
            ViewBag.Reports = reports;

            return View();
        }

        // GET: /Patient/Prescriptions - এই method টি যোগ করুন
        public IActionResult Prescriptions()
        {
            if (!IsPatientLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            var prescriptions = GetAllPrescriptions(userId);
            ViewBag.Prescriptions = prescriptions;
            ViewBag.PatientProfilePicture = GetPatientProfilePicture(userId);

            return View();
        }

        // GET: /Patient/DownloadPrescription
        public IActionResult DownloadPrescription(int id)
        {
            if (!IsPatientLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(
                    "SELECT PrescriptionFile FROM Prescriptions WHERE PrescriptionId = @PrescriptionId AND PatientId = @PatientId",
                    connection);
                cmd.Parameters.AddWithValue("@PrescriptionId", id);
                cmd.Parameters.AddWithValue("@PatientId", userId);

                var fileName = cmd.ExecuteScalar()?.ToString();

                if (!string.IsNullOrEmpty(fileName))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "prescriptions", fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        var fileBytes = System.IO.File.ReadAllBytes(filePath);
                        return File(fileBytes, "application/octet-stream", fileName);
                    }
                }
            }

            TempData["Error"] = "Prescription not found or you don't have permission to access it.";
            return RedirectToAction("Prescriptions");
        }

        // Helper method to get all prescriptions
        private List<dynamic> GetAllPrescriptions(int patientId)
        {
            var prescriptions = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT 
                p.PrescriptionId, 
                p.PrescriptionDate, 
                p.PrescriptionFile, 
                p.Notes,
                p.UploadedAt,
                u.FullName AS PrescribedByName,
                p.IsUploadedByNurse
            FROM Prescriptions p
            INNER JOIN Users u ON p.PrescribedBy = u.UserId
            WHERE p.PatientId = @PatientId
            ORDER BY p.PrescriptionDate DESC", connection);
                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        prescriptions.Add(new
                        {
                            PrescriptionId = reader["PrescriptionId"],
                            PrescriptionDate = Convert.ToDateTime(reader["PrescriptionDate"]).ToString("dd-MMM-yyyy"),
                            PrescriptionFile = reader["PrescriptionFile"].ToString(),
                            Notes = reader["Notes"] != DBNull.Value ? reader["Notes"].ToString() : "",
                            UploadedAt = Convert.ToDateTime(reader["UploadedAt"]).ToString("dd-MMM-yyyy"),
                            PrescribedByName = reader["PrescribedByName"].ToString(),
                            IsUploadedByNurse = Convert.ToBoolean(reader["IsUploadedByNurse"])
                        });
                    }
                }
            }
            return prescriptions;
        }

        // GET: /Patient/Profile (Updated to pass patient data)
        public IActionResult Profile()
        {
            if (!IsPatientLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            var patient = GetPatientDetails(userId);

            // ✅ Session-এ Profile Picture সেট করুন
            if (patient != null && !string.IsNullOrEmpty(patient.ProfilePicture))
            {
                HttpContext.Session.Set("ProfilePicture", System.Text.Encoding.UTF8.GetBytes(patient.ProfilePicture));
            }

            // Pass patient data to view
            ViewBag.Patient = patient;
            ViewBag.PatientProfilePicture = GetPatientProfilePicture(userId);

            return View();
        }

        // POST: /Patient/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ChangePassword([FromBody] ChangePasswordViewModel model)
        {
            if (!IsPatientLoggedIn())
                return Json(new { success = false, message = "Not logged in" });

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return Json(new { success = false, message = "Invalid session" });

            try
            {
                // Validation
                if (string.IsNullOrEmpty(model.CurrentPassword))
                    return Json(new { success = false, message = "Current password is required" });

                if (string.IsNullOrEmpty(model.NewPassword) || model.NewPassword.Length < 6)
                    return Json(new { success = false, message = "New password must be at least 6 characters" });

                if (model.NewPassword != model.ConfirmPassword)
                    return Json(new { success = false, message = "Passwords do not match" });

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Get current password hash
                    var cmd = new SqlCommand(
                        "SELECT PasswordHash FROM Users WHERE UserId = @UserId",
                        connection);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    var currentHash = cmd.ExecuteScalar()?.ToString();

                    // Verify current password
                    var inputHash = HashPassword(model.CurrentPassword);

                    if (currentHash != inputHash)
                    {
                        return Json(new { success = false, message = "Current password is incorrect" });
                    }

                    // Check if new password is same as old
                    var newHash = HashPassword(model.NewPassword);
                    if (currentHash == newHash)
                    {
                        return Json(new { success = false, message = "New password must be different from current password" });
                    }

                    // Update password
                    cmd = new SqlCommand(
                        "UPDATE Users SET PasswordHash = @PasswordHash, UpdatedAt = GETDATE() WHERE UserId = @UserId",
                        connection);
                    cmd.Parameters.AddWithValue("@PasswordHash", newHash);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();

                    return Json(new { success = true, message = "Password changed successfully" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to change password: {ex.Message}" });
            }
        }

        // POST: /Patient/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateProfile(
            string PhoneNumber,
            string EmergencyContact,
            string Address,
            string Height,
            string Weight,
            string MaritalStatus)
        {
            if (!IsPatientLoggedIn())
                return Json(new { success = false, message = "Not logged in" });

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return Json(new { success = false, message = "Invalid session" });

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Update Users table
                    var userCmd = new SqlCommand(@"
                        UPDATE Users 
                        SET PhoneNumber = @PhoneNumber,
                            Address = @Address,
                            UpdatedAt = GETDATE()
                        WHERE UserId = @UserId", connection);

                    userCmd.Parameters.AddWithValue("@PhoneNumber", PhoneNumber ?? (object)DBNull.Value);
                    userCmd.Parameters.AddWithValue("@Address", Address ?? (object)DBNull.Value);
                    userCmd.Parameters.AddWithValue("@UserId", userId);
                    userCmd.ExecuteNonQuery();

                    // Update Patients table
                    var patientCmd = new SqlCommand(@"
                        UPDATE Patients 
                        SET EmergencyContact = @EmergencyContact,
                            Height = @Height,
                            Weight = @Weight,
                            MaritalStatus = @MaritalStatus
                        WHERE PatientId = @PatientId", connection);

                    patientCmd.Parameters.AddWithValue("@EmergencyContact", EmergencyContact ?? (object)DBNull.Value);

                    // Handle Height (convert empty string to DBNull)
                    if (!string.IsNullOrEmpty(Height) && Height != "Not set" && decimal.TryParse(Height, out decimal heightValue))
                    {
                        patientCmd.Parameters.AddWithValue("@Height", heightValue);
                    }
                    else
                    {
                        patientCmd.Parameters.AddWithValue("@Height", DBNull.Value);
                    }

                    // Handle Weight (convert empty string to DBNull)
                    if (!string.IsNullOrEmpty(Weight) && Weight != "Not set" && decimal.TryParse(Weight, out decimal weightValue))
                    {
                        patientCmd.Parameters.AddWithValue("@Weight", weightValue);
                    }
                    else
                    {
                        patientCmd.Parameters.AddWithValue("@Weight", DBNull.Value);
                    }

                    patientCmd.Parameters.AddWithValue("@MaritalStatus", MaritalStatus ?? (object)DBNull.Value);
                    patientCmd.Parameters.AddWithValue("@PatientId", userId);
                    patientCmd.ExecuteNonQuery();
                }

                // Update session data if needed
                var updatedPatient = GetPatientDetails(userId);
                if (updatedPatient != null)
                {
                    // Cast to string explicitly to avoid dynamic dispatch issue
                    string fullName = GetPropertyValue(updatedPatient, "FullName") ?? "";
                    HttpContext.Session.SetString("UserName", fullName);
                }

                return Json(new { success = true, message = "Profile updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Update failed: {ex.Message}" });
            }
        }

        // GET: /Patient/EditProfile
        public IActionResult EditProfile()
        {
            if (!IsPatientLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            var patient = GetPatientDetails(userId);

            if (patient == null)
                return RedirectToAction("Login", "Account");

            // Convert to view model
            var model = new PatientProfileViewModel
            {
                UserId = userId,
                FullName = GetPropertyValue(patient, "FullName") ?? "",
                Email = GetPropertyValue(patient, "Email") ?? "",
                DateOfBirth = GetPropertyValue<DateTime?>(patient, "DateOfBirth"),
                Gender = GetPropertyValue(patient, "Gender") ?? "",
                PhoneNumber = GetPropertyValue(patient, "PhoneNumber") ?? "",
                Address = GetPropertyValue(patient, "Address") ?? "",
                ProfilePicture = GetPropertyValue(patient, "ProfilePicture") ?? "default.webp",
                BloodGroup = GetPropertyValue(patient, "BloodGroup") ?? "Not set",
                Height = GetPropertyValue<decimal?>(patient, "Height"),
                Weight = GetPropertyValue<decimal?>(patient, "Weight"),
                EmergencyContact = GetPropertyValue(patient, "EmergencyContact") ?? "",
                InsuranceInfo = GetPropertyValue(patient, "InsuranceInfo") ?? "",
                Occupation = GetPropertyValue(patient, "Occupation") ?? "",
                MaritalStatus = GetPropertyValue(patient, "MaritalStatus") ?? ""
            };

            return View(model);
        }

        // POST: /Patient/EditProfile
        [HttpPost]
        public IActionResult EditProfile(PatientProfileViewModel model)
        {
            if (!IsPatientLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        // Update Users table
                        var cmd = new SqlCommand(@"
                            UPDATE Users 
                            SET PhoneNumber = @PhoneNumber,
                                Address = @Address,
                                UpdatedAt = GETDATE()
                            WHERE UserId = @UserId", connection);

                        cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address", model.Address ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();

                        // Update Patients table
                        cmd = new SqlCommand(@"
                            UPDATE Patients 
                            SET Height = @Height,
                                Weight = @Weight,
                                EmergencyContact = @EmergencyContact,
                                MaritalStatus = @MaritalStatus
                            WHERE PatientId = @PatientId", connection);

                        cmd.Parameters.AddWithValue("@Height", model.Height.HasValue ? (object)model.Height.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Weight", model.Weight.HasValue ? (object)model.Weight.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@EmergencyContact", model.EmergencyContact ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@MaritalStatus", model.MaritalStatus ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PatientId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction("Profile");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while updating profile: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: /Patient/MedicalHistory
        public IActionResult MedicalHistory()
        {
            if (!IsPatientLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            //Get patient details for sidebar
            var patient = GetPatientDetails(userId);
            ViewBag.Patient = patient;
            ViewBag.PatientProfilePicture = GetPatientProfilePicture(userId);

            // Get diseases history
            var diseases = GetPatientDiseases(userId);
            ViewBag.Diseases = diseases;

            // Get test reports
            var reports = GetPatientTestReports(userId);
            ViewBag.Reports = reports;

            // Get appointment history
            var appointments = GetPatientAppointmentHistory(userId);
            ViewBag.Appointments = appointments;

            return View();
        }

        // GET: /Patient/TestReports
        public IActionResult TestReports()
        {
            if (!IsPatientLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            var reports = GetAllTestReports(userId);
            ViewBag.Reports = reports;
            ViewBag.PatientProfilePicture = GetPatientProfilePicture(userId);

            return View();
        }

        // POST: /Patient/UpdateProfilePicture
        [HttpPost]
        public IActionResult UpdateProfilePicture(IFormFile profilePicture)
        {
            if (!IsPatientLoggedIn())
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

                // ✅ Session-এ আপডেট করুন
                HttpContext.Session.Set("ProfilePicture", System.Text.Encoding.UTF8.GetBytes(uniqueFileName));

                return Json(new { success = true, fileName = uniqueFileName });
            }

            return Json(new { success = false, message = "No file uploaded" });
        }

        // POST: /Patient/RemoveProfilePicture
        [HttpPost]
        public IActionResult RemoveProfilePicture()
        {
            if (!IsPatientLoggedIn())
                return Json(new { success = false, message = "Not logged in" });

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
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
                HttpContext.Session.Set("ProfilePicture", System.Text.Encoding.UTF8.GetBytes("default.webp"));

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Patient/DownloadReport
        public IActionResult DownloadReport(int id)
        {
            if (!IsPatientLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(
                    "SELECT ReportFile FROM TestReports WHERE ReportId = @ReportId AND PatientId = @PatientId",
                    connection);
                cmd.Parameters.AddWithValue("@ReportId", id);
                cmd.Parameters.AddWithValue("@PatientId", userId);

                var fileName = cmd.ExecuteScalar()?.ToString();

                if (!string.IsNullOrEmpty(fileName))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reports", fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        var fileBytes = System.IO.File.ReadAllBytes(filePath);
                        return File(fileBytes, "application/octet-stream", fileName);
                    }
                }
            }

            TempData["Error"] = "Report not found or you don't have permission to access it.";
            return RedirectToAction("TestReports");
        }

        // ==================== HELPER METHODS ====================

        // Helper method to get property values from dynamic objects
        // Profile picture retrieval

        private string HashPassword(string password)
        {
            return AccountController.HashPassword(password); // static method call
        }

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
                    return result?.ToString() ?? "default.webp";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting profile picture: {ex.Message}");
                return "default.webp";
            }
        }

        private string? GetPropertyValue(dynamic obj, string propertyName)
        {
            if (obj == null) return null;
            var property = obj.GetType().GetProperty(propertyName);
            return property?.GetValue(obj)?.ToString();
        }


        // Helper method to get typed property values from dynamic objects
        private T? GetPropertyValue<T>(dynamic obj, string propertyName)
        {
            if (obj == null) return default;
            var property = obj.GetType().GetProperty(propertyName);
            var value = property?.GetValue(obj);

            if (value == null || value is DBNull)
                return default;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        private dynamic GetPatientDetails(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT u.UserId, u.Email, u.FullName, u.DateOfBirth, u.Gender, u.PhoneNumber, 
                   u.Address, u.ProfilePicture, u.CreatedAt, u.Username,
                   p.BloodGroup, p.Height, p.Weight, p.EmergencyContact, 
                   p.InsuranceInfo, p.Occupation, p.MaritalStatus
            FROM Users u
            LEFT JOIN Patients p ON u.UserId = p.PatientId
            WHERE u.UserId = @UserId", connection);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            UserId = reader.GetInt32(0),
                            Email = reader.GetString(1),
                            FullName = reader.GetString(2),
                            DateOfBirth = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                            Gender = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            PhoneNumber = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            Address = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            ProfilePicture = reader.IsDBNull(7) ? "default.webp" : reader.GetString(7), // ✅ default.webp
                            CreatedAt = reader.GetDateTime(8),
                            Username = reader.GetString(9),
                            BloodGroup = reader.IsDBNull(10) ? "Not set" : reader.GetString(10),
                            Height = reader.IsDBNull(11) ? (decimal?)null : reader.GetDecimal(11),
                            Weight = reader.IsDBNull(12) ? (decimal?)null : reader.GetDecimal(12),
                            EmergencyContact = reader.IsDBNull(13) ? "" : reader.GetString(13),
                            InsuranceInfo = reader.IsDBNull(14) ? "" : reader.GetString(14),
                            Occupation = reader.IsDBNull(15) ? "" : reader.GetString(15),
                            MaritalStatus = reader.IsDBNull(16) ? "" : reader.GetString(16)
                        };
                    }
                }
            }
            return null;
        }

        // GetUpcomingAppointments 
        private List<dynamic> GetUpcomingAppointments(int patientId)
        {
            var appointments = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT 
                a.AppointmentId, 
                a.AppointmentDate, 
                CONVERT(VARCHAR(5), a.AppointmentTime, 108) as AppointmentTime, 
                a.Status, 
                a.Reason,
                u.FullName AS DoctorName, 
                d.Specialization
            FROM Appointments a
            INNER JOIN Doctors d ON a.DoctorId = d.DoctorId
            INNER JOIN Users u ON d.DoctorId = u.UserId
            WHERE a.PatientId = @PatientId 
            AND a.AppointmentDate >= CAST(GETDATE() AS DATE)
            AND a.Status IN ('Pending', 'Approved')
            ORDER BY a.AppointmentDate, a.AppointmentTime", connection);
                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new
                        {
                            AppointmentId = reader["AppointmentId"],
                            AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]).ToString("yyyy-MM-dd"),
                            AppointmentTime = reader["AppointmentTime"].ToString(),
                            Status = reader["Status"].ToString(),
                            Reason = reader["Reason"].ToString(),
                            DoctorName = reader["DoctorName"].ToString(),
                            Specialization = reader["Specialization"].ToString()
                        });
                    }
                }
            }
            return appointments;
        }

        private List<dynamic> GetRecentTestReports(int patientId)
        {
            var reports = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT TOP 5 ReportId, ReportName, ReportDate, Notes
                    FROM TestReports
                    WHERE PatientId = @PatientId
                    ORDER BY ReportDate DESC", connection);
                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reports.Add(new
                        {
                            ReportId = reader["ReportId"],
                            ReportName = reader["ReportName"].ToString(),
                            ReportDate = Convert.ToDateTime(reader["ReportDate"]).ToString("yyyy-MM-dd"),
                            Notes = reader["Notes"].ToString()
                        });
                    }
                }
            }
            return reports;
        }

        // Helper method to get patient diseases
        private List<dynamic> GetPatientDiseases(int patientId)
        {
            var diseases = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT d.DiseaseName, pdh.DiagnosedDate, pdh.Status, pdh.Notes,
                           u.FullName AS DoctorName
                    FROM PatientDiseaseHistory pdh
                    INNER JOIN Diseases d ON pdh.DiseaseId = d.DiseaseId
                    LEFT JOIN Users u ON pdh.DiagnosedByDoctor = u.UserId
                    WHERE pdh.PatientId = @PatientId
                    ORDER BY pdh.DiagnosedDate DESC", connection);
                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        diseases.Add(new
                        {
                            DiseaseName = reader["DiseaseName"].ToString(),
                            DiagnosedDate = Convert.ToDateTime(reader["DiagnosedDate"]).ToString("yyyy-MM-dd"),
                            Status = reader["Status"].ToString(),
                            Notes = reader["Notes"].ToString(),
                            DoctorName = reader["DoctorName"] != DBNull.Value ? reader["DoctorName"].ToString() : "Not specified"
                        });
                    }
                }
            }
            return diseases;
        }

        // Helper method to get patient test reports for medical history
        private List<dynamic> GetPatientTestReports(int patientId)
        {
            var reports = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT TOP 5 ReportId, ReportName, ReportDate, ReportFile, Notes
                    FROM TestReports
                    WHERE PatientId = @PatientId
                    ORDER BY ReportDate DESC", connection);
                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reports.Add(new
                        {
                            ReportId = reader["ReportId"],
                            ReportName = reader["ReportName"].ToString(),
                            ReportDate = Convert.ToDateTime(reader["ReportDate"]).ToString("yyyy-MM-dd"),
                            ReportFile = reader["ReportFile"].ToString(),
                            Notes = reader["Notes"].ToString(),
                            FileExists = !string.IsNullOrEmpty(reader["ReportFile"].ToString())
                        });
                    }
                }
            }
            return reports;
        }

        // Helper method to get all test reports
        private List<dynamic> GetAllTestReports(int patientId)
        {
            var reports = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT ReportId, ReportName, ReportDate, ReportFile, Notes
                    FROM TestReports
                    WHERE PatientId = @PatientId
                    ORDER BY ReportDate DESC", connection);
                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reports.Add(new
                        {
                            ReportId = reader["ReportId"],
                            ReportName = reader["ReportName"].ToString(),
                            ReportDate = Convert.ToDateTime(reader["ReportDate"]).ToString("yyyy-MM-dd"),
                            ReportFile = reader["ReportFile"].ToString(),
                            Notes = reader["Notes"].ToString(),
                            FileExists = !string.IsNullOrEmpty(reader["ReportFile"].ToString())
                        });
                    }
                }
            }
            return reports;
        }

        // Add this method in PatientController.cs
        [HttpGet]
        [HttpGet]
        public IActionResult GetPatientStats()
        {
            if (!IsPatientLoggedIn())
                return Json(new { success = false, message = "Not logged in" });

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
                return Json(new { success = false, message = "Invalid session" });

            try
            {
                int totalAppointments = 0;
                int totalReports = 0;
                int totalPrescriptions = 0;
                string? profilePicture = null;
                DateTime? memberSince = null;

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Get total appointments count
                    var appointmentCmd = new SqlCommand(@"
                SELECT COUNT(*) 
                FROM Appointments 
                WHERE PatientId = @PatientId", connection);
                    appointmentCmd.Parameters.AddWithValue("@PatientId", userId);
                    totalAppointments = (int)appointmentCmd.ExecuteScalar();

                    // Get total reports count
                    var reportCmd = new SqlCommand(@"
                SELECT COUNT(*) 
                FROM TestReports 
                WHERE PatientId = @PatientId", connection);
                    reportCmd.Parameters.AddWithValue("@PatientId", userId);
                    totalReports = (int)reportCmd.ExecuteScalar();

                    // Get total prescriptions count
                    var prescriptionCmd = new SqlCommand(@"
                SELECT COUNT(*) 
                FROM Prescriptions 
                WHERE PatientId = @PatientId", connection);
                    prescriptionCmd.Parameters.AddWithValue("@PatientId", userId);
                    totalPrescriptions = (int)prescriptionCmd.ExecuteScalar();

                    // Get profile picture and member since
                    var userCmd = new SqlCommand(@"
                SELECT ProfilePicture, CreatedAt 
                FROM Users 
                WHERE UserId = @UserId", connection);
                    userCmd.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = userCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            profilePicture = reader["ProfilePicture"]?.ToString() ?? "default.webp";
                            memberSince = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    totalAppointments = totalAppointments,
                    totalReports = totalReports,
                    totalPrescriptions = totalPrescriptions,
                    profilePicture = profilePicture,
                    memberSince = memberSince?.ToString("MMM yyyy") ?? DateTime.Now.ToString("MMM yyyy")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Helper method to get appointment history
        private List<dynamic> GetPatientAppointmentHistory(int patientId)
        {
            var appointments = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT 
                a.AppointmentId, 
                a.AppointmentDate, 
                CONVERT(VARCHAR(5), a.AppointmentTime, 108) as AppointmentTime,
                a.Status, 
                a.Reason, 
                a.CreatedAt,
                u.FullName AS DoctorName, 
                d.Specialization
            FROM Appointments a
            INNER JOIN Doctors d ON a.DoctorId = d.DoctorId
            INNER JOIN Users u ON d.DoctorId = u.UserId
            WHERE a.PatientId = @PatientId
            ORDER BY a.AppointmentDate DESC, a.AppointmentTime DESC", connection);
                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new
                        {
                            AppointmentId = reader["AppointmentId"],
                            AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]).ToString("yyyy-MM-dd"),
                            AppointmentTime = reader["AppointmentTime"].ToString(),
                            Status = reader["Status"].ToString(),
                            Reason = reader["Reason"].ToString(),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]).ToString("yyyy-MM-dd"),
                            DoctorName = reader["DoctorName"].ToString(),
                            Specialization = reader["Specialization"].ToString()
                        });
                    }
                }
            }
            return appointments;
        }
    }
}