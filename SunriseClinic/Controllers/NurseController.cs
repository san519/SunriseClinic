using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http; // ✅ এই using যোগ করুন
using SunriseClinic.Models;
using System.Data;

namespace SunriseClinic.Controllers
{
    public class NurseController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public NurseController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Check if nurse is logged in
        private bool IsNurseLoggedIn()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");
            return !string.IsNullOrEmpty(userId) && userType == "Nurse";
        }


        // NurseController.cs - এই methods গুলো যোগ করুন

        // GET: /Nurse/Patients (Search with pagination)
        public IActionResult Patients(string search = "", int page = 1, int pageSize = 10)
        {
            if (!IsNurseLoggedIn())
                return RedirectToAction("Login", "Account");

            var patients = GetPatientsWithPagination(search, page, pageSize, out int totalRecords);

            ViewBag.SearchTerm = search;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            return View(patients);
        }
        // NurseController.cs - PatientDetails method টি এভাবে দেখতে হবে:

        [HttpGet]
        public IActionResult PatientDetails(int id)
        {
            if (!IsNurseLoggedIn())
                return RedirectToAction("Login", "Account");

            var patient = GetPatientDetails(id);
            var appointments = GetPatientAppointments(id);
            var prescriptions = GetPatientPrescriptions(id);
            var testReports = GetPatientTestReports(id);
            var diseases = GetPatientDiseases(id);

            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient not found";
                return RedirectToAction("Patients");
            }

            ViewBag.Patient = patient;
            ViewBag.Appointments = appointments;
            ViewBag.Prescriptions = prescriptions;
            ViewBag.TestReports = testReports;
            ViewBag.Diseases = diseases;

            return View();
        }

        // GET: /Nurse/DownloadPrescription/{id}
        public IActionResult DownloadPrescription(int id)
        {
            if (!IsNurseLoggedIn())
                return RedirectToAction("Login", "Account");

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(
                    "SELECT PrescriptionFile FROM Prescriptions WHERE PrescriptionId = @PrescriptionId",
                    connection);
                cmd.Parameters.AddWithValue("@PrescriptionId", id);

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

            TempData["ErrorMessage"] = "Prescription file not found";
            return RedirectToAction("Patients");
        }

        // POST: /Nurse/DeletePrescription/{id}
        [HttpPost]
        public IActionResult DeletePrescription(int id, int patientId)
        {
            if (!IsNurseLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "DELETE FROM Prescriptions WHERE PrescriptionId = @PrescriptionId",
                        connection);
                    cmd.Parameters.AddWithValue("@PrescriptionId", id);
                    cmd.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = "Prescription deleted successfully";
                return RedirectToAction("PatientDetails", new { id = patientId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Delete failed: " + ex.Message;
                return RedirectToAction("PatientDetails", new { id = patientId });
            }
        }

        // GET: /Nurse/DownloadTestReport/{id}
        public IActionResult DownloadTestReport(int id)
        {
            if (!IsNurseLoggedIn())
                return RedirectToAction("Login", "Account");

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(
                    "SELECT ReportFile FROM TestReports WHERE ReportId = @ReportId",
                    connection);
                cmd.Parameters.AddWithValue("@ReportId", id);

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

            TempData["ErrorMessage"] = "Test report file not found";
            return RedirectToAction("Patients");
        }

        // POST: /Nurse/DeleteTestReport/{id}
        [HttpPost]
        public IActionResult DeleteTestReport(int id, int patientId)
        {
            if (!IsNurseLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "DELETE FROM TestReports WHERE ReportId = @ReportId",
                        connection);
                    cmd.Parameters.AddWithValue("@ReportId", id);
                    cmd.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = "Test report deleted successfully";
                return RedirectToAction("PatientDetails", new { id = patientId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Delete failed: " + ex.Message;
                return RedirectToAction("PatientDetails", new { id = patientId });
            }
        }

        // GET: /Nurse/Profile
        public IActionResult Profile()
        {
            if (!IsNurseLoggedIn())
                return RedirectToAction("Login", "Account");

            var nurseId = int.Parse(HttpContext.Session.GetString("UserId"));
            var nurse = GetNurseDetailsWithPicture(nurseId);

            if (nurse == null)
            {
                TempData["ErrorMessage"] = "Nurse profile not found";
                return RedirectToAction("Dashboard");
            }

            // ✅ সেশনেও প্রোফাইল পিকচার সেভ করুন
            SessionExtensions.SetString(HttpContext.Session, "ProfilePicture", nurse.ProfilePicture);

            ViewBag.ProfilePicture = nurse.ProfilePicture ?? "default.jpg";
            ViewBag.Nurse = nurse;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.DisplayId = HttpContext.Session.GetString("DisplayId");

            return View();
        }

        // NurseController.cs - এই মেথডটি ক্লাসের ভিতরে যোগ করুন

        // POST: /Nurse/UpdateProfile
        [HttpPost]
        public IActionResult UpdateProfile(NurseProfileViewModel model)
        {
            try
            {
                if (!IsNurseLoggedIn())
                {
                    return Json(new { success = false, message = "Not logged in" });
                }

                var nurseId = int.Parse(HttpContext.Session.GetString("UserId"));

                // Validation
                if (string.IsNullOrEmpty(model.PhoneNumber))
                    return Json(new { success = false, message = "Phone number is required" });

                if (string.IsNullOrEmpty(model.Address))
                    return Json(new { success = false, message = "Address is required" });

                if (string.IsNullOrEmpty(model.Gender))
                    return Json(new { success = false, message = "Gender is required" });

                if (string.IsNullOrEmpty(model.Department))
                    return Json(new { success = false, message = "Department is required" });

                if (string.IsNullOrEmpty(model.ShiftTime))
                    return Json(new { success = false, message = "Shift time is required" });

                if (model.ExperienceYears < 0 || model.ExperienceYears > 50)
                    return Json(new { success = false, message = "Experience years must be between 0 and 50" });

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Begin transaction
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Update Users table
                            var userCmd = new SqlCommand(@"
                        UPDATE Users 
                        SET PhoneNumber = @PhoneNumber,
                            Address = @Address,
                            Gender = @Gender,
                            DateOfBirth = @DateOfBirth
                        WHERE UserId = @UserId",
                                connection, transaction);

                            userCmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
                            userCmd.Parameters.AddWithValue("@Address", model.Address);
                            userCmd.Parameters.AddWithValue("@Gender", model.Gender);

                            if (model.DateOfBirth.HasValue)
                                userCmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth.Value);
                            else
                                userCmd.Parameters.AddWithValue("@DateOfBirth", DBNull.Value);

                            userCmd.Parameters.AddWithValue("@UserId", nurseId);

                            userCmd.ExecuteNonQuery();

                            // Update Nurses table
                            var nurseCmd = new SqlCommand(@"
                        UPDATE Nurses 
                        SET Department = @Department,
                            ShiftTime = @ShiftTime,
                            ExperienceYears = @ExperienceYears
                        WHERE NurseId = @NurseId",
                                connection, transaction);

                            nurseCmd.Parameters.AddWithValue("@Department", model.Department);
                            nurseCmd.Parameters.AddWithValue("@ShiftTime", model.ShiftTime);
                            nurseCmd.Parameters.AddWithValue("@ExperienceYears", model.ExperienceYears);
                            nurseCmd.Parameters.AddWithValue("@NurseId", nurseId);

                            nurseCmd.ExecuteNonQuery();

                            transaction.Commit();

                            // Update session data
                            HttpContext.Session.SetString("PhoneNumber", model.PhoneNumber);

                            return Json(new
                            {
                                success = true,
                                message = "Profile updated successfully"
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"ERROR in UpdateProfile transaction: {ex.Message}");
                            return Json(new
                            {
                                success = false,
                                message = $"Update failed: {ex.Message}"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in UpdateProfile: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}"
                });
            }
        }

        // POST: /Nurse/UpdateProfilePicture
        [HttpPost]
        public IActionResult UpdateProfilePicture(IFormFile profilePicture)
        {
            try
            {
                if (!IsNurseLoggedIn())
                {
                    return StatusCode(401, new { success = false, message = "Not logged in" });
                }

                var nurseId = int.Parse(HttpContext.Session.GetString("UserId"));

                if (profilePicture == null || profilePicture.Length == 0)
                    return BadRequest(new { success = false, message = "No file selected" });

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest(new { success = false, message = "Invalid file type. Only JPG, PNG, GIF allowed" });

                // Validate file size (max 5MB)
                if (profilePicture.Length > 5 * 1024 * 1024)
                    return BadRequest(new { success = false, message = "File size too large (max 5MB)" });

                // Generate unique filename
                var fileName = $"nurse_{nurseId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                var uploadsFolder = Path.Combine("wwwroot", "uploads", "profile-pictures");

                // Create directory if not exists
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    profilePicture.CopyTo(stream);
                }

                // Update database
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "UPDATE Users SET ProfilePicture = @ProfilePicture WHERE UserId = @UserId",
                        connection);
                    cmd.Parameters.AddWithValue("@ProfilePicture", fileName);
                    cmd.Parameters.AddWithValue("@UserId", nurseId);
                    cmd.ExecuteNonQuery();
                }

                // ✅ Update session - সঠিক ভাবে
                SessionExtensions.SetString(HttpContext.Session, "ProfilePicture", fileName);

                return Ok(new
                {
                    success = true,
                    fileName = fileName,
                    message = "Profile picture updated successfully"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in UpdateProfilePicture: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error uploading picture. Please try again."
                });
            }
        }

        // POST: /Nurse/RemoveProfilePicture
        [HttpPost]
        public IActionResult RemoveProfilePicture()
        {
            try
            {
                if (!IsNurseLoggedIn())
                {
                    return StatusCode(401, new { success = false, message = "Not logged in" });
                }

                var nurseId = int.Parse(HttpContext.Session.GetString("UserId"));

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "UPDATE Users SET ProfilePicture = 'default.jpg' WHERE UserId = @UserId",
                        connection);
                    cmd.Parameters.AddWithValue("@UserId", nurseId);
                    cmd.ExecuteNonQuery();
                }

                // ✅ Update session - সঠিক ভাবে
                SessionExtensions.SetString(HttpContext.Session, "ProfilePicture", "default.jpg");

                return Ok(new
                {
                    success = true,
                    message = "Profile picture removed"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in RemoveProfilePicture: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error removing picture. Please try again."
                });
            }
        }

        // Dashboard method
        public IActionResult Dashboard()
        {
            if (!IsNurseLoggedIn())
                return RedirectToAction("Login", "Account");

            var nurseId = int.Parse(HttpContext.Session.GetString("UserId"));

            // Get nurse details
            var nurse = GetNurseDetailsWithPicture(nurseId);
            ViewBag.Nurse = nurse;

            // ✅ সেশনেও প্রোফাইল পিকচার সেভ করুন
            if (nurse != null && !string.IsNullOrEmpty(nurse.ProfilePicture))
            {
                SessionExtensions.SetString(HttpContext.Session, "ProfilePicture", nurse.ProfilePicture);
            }

            // Get dashboard stats
            ViewBag.Stats = GetNurseStats(nurseId);

            // Get today's appointments
            ViewBag.TodaysAppointments = GetTodaysAppointments();

            // Get pending appointments
            ViewBag.PendingAppointments = GetPendingAppointments();

            // Get recent patients
            ViewBag.RecentPatients = GetRecentPatients();
            ViewBag.AppointmentManagementLink = "/AppointmentManagement/Index";

            return View();
        }

        // নতুন helper methods
        private List<dynamic> GetPendingAppointments()
        {
            var appointments = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT TOP 5 a.AppointmentId, a.AppointmentDate, a.AppointmentTime, a.Reason,
                   p.FullName as PatientName, d.FullName as DoctorName
            FROM Appointments a
            INNER JOIN Users p ON a.PatientId = p.UserId
            INNER JOIN Users d ON a.DoctorId = d.UserId
            WHERE a.Status = 'Pending'
            AND a.AppointmentDate >= CAST(GETDATE() AS DATE)
            ORDER BY a.AppointmentDate, a.AppointmentTime",
                    connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new
                        {
                            AppointmentId = reader.GetInt32(0),
                            AppointmentDate = reader.GetDateTime(1).ToString("dd-MMM-yyyy"),
                            AppointmentTime = reader.GetTimeSpan(2).ToString(@"hh\:mm"),
                            Reason = reader.GetString(3),
                            PatientName = reader.GetString(4),
                            DoctorName = reader.GetString(5)
                        });
                    }
                }
            }
            return appointments;
        }


        // POST: /Nurse/UpdateAppointmentStatus
        [HttpPost]
        public IActionResult UpdateAppointmentStatus(int appointmentId, string status)
        {
            if (!IsNurseLoggedIn())
                return Json(new { success = false, message = "Not logged in" });

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "UPDATE Appointments SET Status = @Status WHERE AppointmentId = @Id",
                        connection);
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@Id", appointmentId);
                    cmd.ExecuteNonQuery();
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Nurse/UploadPrescription
        // NurseController.cs - UploadPrescription মেথডে
        public IActionResult UploadPrescription(int patientId)
        {
            if (!IsNurseLoggedIn())
                return RedirectToAction("Login", "Account");

            // Patient ডিটেইলস নিয়ে আসুন
            var patient = GetPatientDetails(patientId);

            ViewBag.PatientId = patientId;
            ViewBag.Patient = patient;
            ViewBag.DisplayId = GenerateDisplayId(patientId, "Patient"); // ✅ DisplayId পাঠান

            return View();
        }

        // POST: /Nurse/UploadPrescription
        [HttpPost]
        public IActionResult UploadPrescription(int patientId, IFormFile file, string notes)
        {
            if (!IsNurseLoggedIn())
                return RedirectToAction("Login", "Account");

            var nurseId = int.Parse(HttpContext.Session.GetString("UserId"));

            try
            {
                // Save file
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine("wwwroot/uploads/prescriptions", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                // Save to database
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(@"
                        INSERT INTO Prescriptions (PatientId, PrescriptionDate, PrescriptionFile, 
                                                  PrescribedBy, Notes, IsUploadedByNurse)
                        VALUES (@PatientId, GETDATE(), @FileName, @NurseId, @Notes, 1)",
                        connection);
                    cmd.Parameters.AddWithValue("@PatientId", patientId);
                    cmd.Parameters.AddWithValue("@FileName", fileName);
                    cmd.Parameters.AddWithValue("@NurseId", nurseId);
                    cmd.Parameters.AddWithValue("@Notes", notes ?? "");
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Prescription uploaded successfully";
                return RedirectToAction("PatientDetails", new { id = patientId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Upload failed: " + ex.Message;
                return RedirectToAction("UploadPrescription", new { patientId });
            }
        }

        // GET: /Nurse/TodaysAllSchedules
        public IActionResult TodaysAllSchedules()
        {
            if (!IsNurseLoggedIn())
                return RedirectToAction("Login", "Account");

            ViewBag.TodaysAllAppointments = GetTodaysAllAppointments();
            return View();
        }

        // Helper method - GetTodaysAllAppointments (All Schedule পেজের জন্য)
        private List<dynamic> GetTodaysAllAppointments()
        {
            var appointments = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT a.AppointmentId, a.AppointmentTime, a.Status, a.Reason,
                   p.FullName as PatientName, d.FullName as DoctorName
            FROM Appointments a
            INNER JOIN Users p ON a.PatientId = p.UserId
            INNER JOIN Users d ON a.DoctorId = d.UserId
            WHERE a.AppointmentDate = CAST(GETDATE() AS DATE)
            AND a.Status = 'Approved'  -- All Schedule পেজেও শুধু Approved দেখাবে
            ORDER BY a.AppointmentTime",
                    connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new
                        {
                            AppointmentId = reader.GetInt32(0),
                            AppointmentTime = reader.GetTimeSpan(1).ToString(@"hh\:mm"),
                            Status = reader.GetString(2),
                            Reason = reader.GetString(3),
                            PatientName = reader.GetString(4),
                            DoctorName = reader.GetString(5)
                        });
                    }
                }
            }
            return appointments;
        }

        // GET: /Nurse/UploadTestReport
        public IActionResult UploadTestReport(int patientId)
        {
            if (!IsNurseLoggedIn())
                return RedirectToAction("Login", "Account");

            ViewBag.PatientId = patientId;
            ViewBag.DisplayId = GenerateDisplayId(patientId, "Patient");
            return View();
        }

        // POST: /Nurse/UploadTestReport
        [HttpPost]
        public IActionResult UploadTestReport(int patientId, IFormFile file, string reportName, string notes)
        {
            if (!IsNurseLoggedIn())
                return RedirectToAction("Login", "Account");

            var nurseId = int.Parse(HttpContext.Session.GetString("UserId"));

            try
            {
                // Save file
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine("wwwroot/uploads/reports", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                // Save to database
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(@"
                        INSERT INTO TestReports (PatientId, ReportName, ReportDate, ReportFile, 
                                                UploadedBy, Notes, IsUploadedByNurse)
                        VALUES (@PatientId, @ReportName, GETDATE(), @FileName, @NurseId, @Notes, 1)",
                        connection);
                    cmd.Parameters.AddWithValue("@PatientId", patientId);
                    cmd.Parameters.AddWithValue("@ReportName", reportName);
                    cmd.Parameters.AddWithValue("@FileName", fileName);
                    cmd.Parameters.AddWithValue("@NurseId", nurseId);
                    cmd.Parameters.AddWithValue("@Notes", notes ?? "");
                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Test report uploaded successfully";
                return RedirectToAction("PatientDetails", new { id = patientId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Upload failed: " + ex.Message;
                return RedirectToAction("UploadTestReport", new { patientId });
            }
        }

        // ==================== HELPER METHODS ====================

        // GetNurseStats মেথড
        private dynamic GetNurseStats(int nurseId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            -- Appointment Stats
            DECLARE @PendingAppointments INT = (
                SELECT COUNT(*) FROM Appointments 
                WHERE Status = 'Pending' 
                AND AppointmentDate >= CAST(GETDATE() AS DATE)
            );
            
            DECLARE @TodaysAppointments INT = (
                SELECT COUNT(*) FROM Appointments 
                WHERE AppointmentDate = CAST(GETDATE() AS DATE)
            );
            
            DECLARE @TodaysApproved INT = (
                SELECT COUNT(*) FROM Appointments 
                WHERE AppointmentDate = CAST(GETDATE() AS DATE)
                AND Status = 'Approved'
            );
            
            DECLARE @EmergencyAppointments INT = (
                SELECT COUNT(*) FROM Appointments 
                WHERE IsEmergency = 1 
                AND Status IN ('Pending', 'Approved') 
                AND AppointmentDate >= CAST(GETDATE() AS DATE)
            );
            
            -- Nurse-specific stats
            SELECT 
                (SELECT COUNT(*) FROM Users WHERE UserType = 'Patient') as TotalPatients,
                @PendingAppointments as PendingAppointments,
                @TodaysAppointments as TodaysAppointments,
                @TodaysApproved as TodaysApproved,
                @EmergencyAppointments as EmergencyAppointments,
                (SELECT COUNT(*) FROM Prescriptions WHERE PrescribedBy = @NurseId) as MyPrescriptions,
                (SELECT COUNT(*) FROM TestReports WHERE UploadedBy = @NurseId) as MyTestReports",
                    connection);
                cmd.Parameters.AddWithValue("@NurseId", nurseId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            TotalPatients = reader.GetInt32(0),
                            PendingAppointments = reader.GetInt32(1),
                            TodaysAppointments = reader.GetInt32(2),
                            TodaysApproved = reader.GetInt32(3),
                            EmergencyAppointments = reader.GetInt32(4),
                            MyPrescriptions = reader.GetInt32(5),
                            MyTestReports = reader.GetInt32(6)
                        };
                    }
                }
            }
            return null;
        }

        // Helper method - GetTodaysAppointments (Dashboard এর জন্য)
        private List<dynamic> GetTodaysAppointments()
        {
            var appointments = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT a.AppointmentId, a.AppointmentTime, a.Status, a.Reason,
                   p.FullName as PatientName, d.FullName as DoctorName
            FROM Appointments a
            INNER JOIN Users p ON a.PatientId = p.UserId
            INNER JOIN Users d ON a.DoctorId = d.UserId
            WHERE a.AppointmentDate = CAST(GETDATE() AS DATE)
            AND a.Status = 'Approved'  -- শুধুমাত্র Approved appointments
            ORDER BY a.AppointmentTime",
                    connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new
                        {
                            AppointmentId = reader.GetInt32(0),
                            AppointmentTime = reader.GetTimeSpan(1).ToString(@"hh\:mm"),
                            Status = reader.GetString(2),
                            Reason = reader.GetString(3),
                            PatientName = reader.GetString(4),
                            DoctorName = reader.GetString(5)
                        });
                    }
                }
            }
            return appointments;
        }

        private List<dynamic> GetRecentPatients()
        {
            var patients = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT TOP 10 UserId, FullName, Email, PhoneNumber, CreatedAt
                    FROM Users 
                    WHERE UserType = 'Patient' 
                    ORDER BY CreatedAt DESC",
                    connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        patients.Add(new
                        {
                            UserId = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            Email = reader.GetString(2),
                            PhoneNumber = reader.GetString(3),
                            CreatedAt = reader.GetDateTime(4).ToString("dd-MMM-yyyy")
                        });
                    }
                }
            }
            return patients;
        }

        // Helper method - Get nurse details with picture
        private dynamic GetNurseDetailsWithPicture(int nurseId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(@"
                SELECT 
                    u.UserId, 
                    u.FullName, 
                    u.Email, 
                    ISNULL(u.PhoneNumber, '') as PhoneNumber, 
                    ISNULL(u.Address, '') as Address,
                    ISNULL(u.Gender, '') as Gender, 
                    u.DateOfBirth, 
                    ISNULL(u.ProfilePicture, 'default.jpg') as ProfilePicture,
                    u.CreatedAt,
                    ISNULL(n.Department, '') as Department, 
                    ISNULL(n.ShiftTime, '') as ShiftTime,  
                    ISNULL(n.NurseLicense, '') as NurseLicense, 
                    ISNULL(n.ExperienceYears, 0) as ExperienceYears,
                    u.IsActive
                FROM Users u
                INNER JOIN Nurses n ON u.UserId = n.NurseId
                WHERE u.UserId = @NurseId",
                        connection);
                    cmd.Parameters.AddWithValue("@NurseId", nurseId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Database থেকে shift time পড়ুন
                            var shiftTimeFromDb = reader.GetString(10);

                            // এখানেই format conversion করুন
                            var convertedShiftTime = !string.IsNullOrEmpty(shiftTimeFromDb)
                                ? shiftTimeFromDb switch
                                {
                                    "Morning (8AM-4PM)" => "Day Shift (8 AM - 4 PM)",
                                    "Evening (4PM-12AM)" => "Evening Shift (4 PM - 12 AM)",
                                    "Night (12AM-8AM)" => "Night Shift (12 AM - 8 AM)",
                                    "Full Day" => "Full Time",
                                    _ => shiftTimeFromDb
                                }
                                : "";

                            return new
                            {
                                UserId = reader.GetInt32(0),
                                FullName = reader.GetString(1),
                                Email = reader.GetString(2),
                                PhoneNumber = reader.GetString(3),
                                Address = reader.GetString(4),
                                Gender = reader.GetString(5),
                                DateOfBirth = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                                ProfilePicture = reader.GetString(7),
                                CreatedAt = reader.GetDateTime(8),
                                Department = reader.GetString(9),
                                ShiftTime = convertedShiftTime,  // ✅ Already converted
                                NurseLicense = reader.GetString(11),
                                ExperienceYears = reader.GetInt32(12),
                                IsActive = reader.GetBoolean(13)
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetNurseDetailsWithPicture: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            return null;
        }

        private dynamic GetNurseDetails(int nurseId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT u.*, n.Department, n.ShiftTime
                    FROM Users u
                    LEFT JOIN Nurses n ON u.UserId = n.NurseId
                    WHERE u.UserId = @NurseId",
                    connection);
                cmd.Parameters.AddWithValue("@NurseId", nurseId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            UserId = reader.GetInt32(0),
                            FullName = reader.GetString(3),
                            Email = reader.GetString(2),
                            PhoneNumber = reader.GetString(6),
                            Address = reader["Address"]?.ToString(),
                            Department = reader["Department"]?.ToString(),
                            ShiftTime = reader["ShiftTime"]?.ToString()
                        };
                    }
                }
            }
            return null;
        }

        private List<dynamic> GetAllPatients()
        {
            var patients = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT u.UserId, u.FullName, u.Email, u.PhoneNumber, 
                           p.BloodGroup, p.EmergencyContact
                    FROM Users u
                    LEFT JOIN Patients p ON u.UserId = p.PatientId
                    WHERE u.UserType = 'Patient'
                    ORDER BY u.FullName",
                    connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        patients.Add(new
                        {
                            UserId = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            Email = reader.GetString(2),
                            PhoneNumber = reader.GetString(3),
                            BloodGroup = reader["BloodGroup"]?.ToString() ?? "Not set",
                            EmergencyContact = reader["EmergencyContact"]?.ToString() ?? "Not set"
                        });
                    }
                }
            }
            return patients;
        }


        // NurseController.cs - Helper Methods

        private List<dynamic> GetPatientsWithPagination(string search, int page, int pageSize, out int totalRecords)
        {
            var patients = new List<dynamic>();
            totalRecords = 0;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // ✅ Search condition builder
                    var whereConditions = new List<string>();
                    var sqlParameters = new List<SqlParameter>();

                    // Base condition
                    whereConditions.Add("u.UserType = 'Patient'");
                    whereConditions.Add("u.IsActive = 1");

                    // Handle search
                    if (!string.IsNullOrEmpty(search))
                    {
                        // Check if search is a Patient ID (starts with P followed by numbers)
                        if (search.StartsWith("P", StringComparison.OrdinalIgnoreCase) &&
                            search.Length > 1 &&
                            search.Substring(1).All(char.IsDigit))
                        {
                            // Extract the numeric part after 'P'
                            string numbers = search.Substring(1);
                            if (int.TryParse(numbers, out int displayIdNumber))
                            {
                                // Convert display ID to user ID (reverse calculation: UserId = DisplayIdNumber - 99000)
                                int calculatedUserId = displayIdNumber - 99000;
                                if (calculatedUserId > 0)
                                {
                                    whereConditions.Add("u.UserId = @CalculatedUserId");
                                    sqlParameters.Add(new SqlParameter("@CalculatedUserId", calculatedUserId));
                                }
                            }
                        }
                        else
                        {
                            // Normal search by name, email, phone
                            whereConditions.Add(@"
                        (u.FullName LIKE @SearchPattern OR 
                         u.Email LIKE @SearchPattern OR 
                         u.PhoneNumber LIKE @SearchPattern OR
                         ('P' + CAST((u.UserId + 99000) AS VARCHAR(10))) LIKE @SearchPattern)");
                            sqlParameters.Add(new SqlParameter("@SearchPattern", "%" + search + "%"));
                        }
                    }

                    // Build WHERE clause
                    string whereClause = whereConditions.Any() ? "WHERE " + string.Join(" AND ", whereConditions) : "";

                    // ✅ Get total count
                    var countQuery = $@"
                SELECT COUNT(*)
                FROM Users u
                LEFT JOIN Patients p ON u.UserId = p.PatientId
                {whereClause}";

                    using (var countCmd = new SqlCommand(countQuery, connection))
                    {
                        // Clone parameters manually
                        foreach (var param in sqlParameters)
                        {
                            // Manually create new parameter with same values
                            var newParam = new SqlParameter(param.ParameterName, param.SqlDbType)
                            {
                                Value = param.Value,
                                Size = param.Size,
                                Direction = param.Direction
                            };
                            countCmd.Parameters.Add(newParam);
                        }
                        totalRecords = (int)countCmd.ExecuteScalar();
                        Console.WriteLine($"Total patients found: {totalRecords} for search: '{search}'");
                    }

                    // ✅ Get paginated data
                    var dataQuery = $@"
                SELECT 
                    u.UserId, 
                    u.FullName, 
                    u.Email, 
                    ISNULL(u.PhoneNumber, '') as PhoneNumber, 
                    ISNULL(u.Gender, '') as Gender, 
                    u.CreatedAt,
                    u.UserType,
                    ISNULL(p.BloodGroup, 'Not set') as BloodGroup, 
                    ISNULL(p.EmergencyContact, 'Not set') as EmergencyContact
                FROM Users u
                LEFT JOIN Patients p ON u.UserId = p.PatientId
                {whereClause}
                ORDER BY u.FullName
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    using (var dataCmd = new SqlCommand(dataQuery, connection))
                    {
                        // Add search parameters
                        foreach (var param in sqlParameters)
                        {
                            // Manually create new parameter with same values
                            var newParam = new SqlParameter(param.ParameterName, param.SqlDbType)
                            {
                                Value = param.Value,
                                Size = param.Size,
                                Direction = param.Direction
                            };
                            dataCmd.Parameters.Add(newParam);
                        }

                        // Add pagination parameters
                        dataCmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                        dataCmd.Parameters.AddWithValue("@PageSize", pageSize);

                        using (var reader = dataCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int userId = reader.GetInt32(0);
                                string userType = reader.GetString(6); // UserType

                                patients.Add(new
                                {
                                    UserId = userId,
                                    FullName = reader.GetString(1),
                                    Email = reader.GetString(2),
                                    PhoneNumber = reader.GetString(3),
                                    Gender = reader.GetString(4),
                                    CreatedAt = reader.GetDateTime(5).ToString("dd-MMM-yyyy"),
                                    BloodGroup = reader.GetString(7),
                                    EmergencyContact = reader.GetString(8),
                                    DisplayId = GenerateDisplayId(userId, userType)
                                });
                            }
                            Console.WriteLine($"Patients loaded: {patients.Count}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetPatientsWithPagination: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            return patients;
        }

        private dynamic GetPatientDetails(int patientId)
{
    try
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            
            var patientQuery = @"
                SELECT 
                    u.UserId, 
                    ISNULL(u.Username, '') as Username,
                    ISNULL(u.Email, '') as Email,
                    ISNULL(u.FullName, '') as FullName, 
                    u.DateOfBirth, 
                    ISNULL(u.Gender, '') as Gender, 
                    ISNULL(u.PhoneNumber, '') as PhoneNumber, 
                    ISNULL(u.Address, '') as Address, 
                    ISNULL(u.ProfilePicture, 'default.jpg') as ProfilePicture, 
                    u.CreatedAt,
                    ISNULL(u.UserType, 'Patient') as UserType,
                    ISNULL(p.BloodGroup, 'Not set') as BloodGroup, 
                    p.Height, 
                    p.Weight, 
                    ISNULL(p.EmergencyContact, '') as EmergencyContact, 
                    ISNULL(p.InsuranceInfo, '') as InsuranceInfo, 
                    ISNULL(p.Occupation, '') as Occupation, 
                    ISNULL(p.MaritalStatus, '') as MaritalStatus
                FROM Users u
                LEFT JOIN Patients p ON u.UserId = p.PatientId
                WHERE u.UserId = @PatientId 
                AND u.UserType = 'Patient' 
                AND u.IsActive = 1";

            using (var patientCmd = new SqlCommand(patientQuery, connection))
            {
                patientCmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = patientCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // ✅ ALL columns পড়ার আগে NULL check করুন
                        int userId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        string username = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        string email = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        string fullName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        DateTime? dateOfBirth = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);
                        string gender = reader.IsDBNull(5) ? "" : reader.GetString(5);
                        string phoneNumber = reader.IsDBNull(6) ? "" : reader.GetString(6);
                        string address = reader.IsDBNull(7) ? "" : reader.GetString(7);
                        string profilePicture = reader.IsDBNull(8) ? "default.jpg" : reader.GetString(8);
                        DateTime createdAt = reader.IsDBNull(9) ? DateTime.Now : reader.GetDateTime(9);
                        string userType = reader.IsDBNull(10) ? "Patient" : reader.GetString(10);
                        string bloodGroup = reader.IsDBNull(11) ? "Not set" : reader.GetString(11);
                        
                        // Height, Weight এর জন্য বিশেষ treatment (decimal nullable)
                        decimal? height = null;
                        decimal? weight = null;
                        
                        if (!reader.IsDBNull(12))
                        {
                            try { height = reader.GetDecimal(12); }
                            catch { height = null; }
                        }
                        
                        if (!reader.IsDBNull(13))
                        {
                            try { weight = reader.GetDecimal(13); }
                            catch { weight = null; }
                        }
                        
                        string emergencyContact = reader.IsDBNull(14) ? "" : reader.GetString(14);
                        string insuranceInfo = reader.IsDBNull(15) ? "" : reader.GetString(15);
                        string occupation = reader.IsDBNull(16) ? "" : reader.GetString(16);
                        string maritalStatus = reader.IsDBNull(17) ? "" : reader.GetString(17);
                        
                        return new
                        {
                            UserId = userId,
                            Username = username,
                            Email = email,
                            FullName = fullName,
                            DateOfBirth = dateOfBirth,
                            Gender = gender,
                            PhoneNumber = phoneNumber,
                            Address = address,
                            ProfilePicture = profilePicture,
                            CreatedAt = createdAt,
                            BloodGroup = bloodGroup,
                            Height = height?.ToString() ?? "",
                            Weight = weight?.ToString() ?? "",
                            EmergencyContact = emergencyContact,
                            InsuranceInfo = insuranceInfo,
                            Occupation = occupation,
                            MaritalStatus = maritalStatus,
                            DisplayId = GenerateDisplayId(userId, userType)
                        };
                    }
                    else
                    {
                        Console.WriteLine($"Patient not found with ID: {patientId}");
                        return null;
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERROR in GetPatientDetails: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        return null;
    }
}

        // এই মেথডটি NurseController ক্লাসের ভিতরে যোগ করুন
        private string GenerateDisplayId(int userId, string userType)
        {
            return userType switch
            {
                "Patient" => "P" + (userId + 99000).ToString(),
                "Doctor" => "D" + (userId + 9000).ToString(),
                "Nurse" => "N" + (userId + 9000).ToString(),
                "Admin" => "A" + userId.ToString(),
                _ => userId.ToString()
            };
        }

        private List<dynamic> GetPatientAppointments(int patientId)
        {
            var appointments = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT a.AppointmentId, 
                   FORMAT(a.AppointmentDate, 'dd-MMM-yyyy') as AppointmentDate,
                   CONVERT(VARCHAR(5), a.AppointmentTime, 108) as AppointmentTime,
                   a.Status, a.Reason, a.IsEmergency,
                   d.FullName as DoctorName, d2.Specialization
            FROM Appointments a
            INNER JOIN Users d ON a.DoctorId = d.UserId
            INNER JOIN Doctors d2 ON d.UserId = d2.DoctorId
            WHERE a.PatientId = @PatientId
            ORDER BY a.AppointmentDate DESC, a.AppointmentTime DESC",
                    connection);
                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new
                        {
                            AppointmentId = reader.GetInt32(0),
                            AppointmentDate = reader.GetString(1),
                            AppointmentTime = reader.GetString(2),
                            Status = reader.GetString(3),
                            Reason = reader.GetString(4),
                            IsEmergency = reader.GetBoolean(5),
                            DoctorName = reader.GetString(6),
                            Specialization = reader.GetString(7)
                        });
                    }
                }
            }
            return appointments;
        }

        // GetPatientPrescriptions মেথড - ISNULL ব্যবহার করুন
        private List<dynamic> GetPatientPrescriptions(int patientId)
        {
            var prescriptions = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT 
                p.PrescriptionId, 
                CONVERT(VARCHAR, p.PrescriptionDate, 106) as PrescriptionDate,
                p.PrescriptionFile, 
                ISNULL(p.Notes, '') as Notes,
                CONVERT(VARCHAR, p.UploadedAt, 106) + ' ' + CONVERT(VARCHAR, p.UploadedAt, 108) as UploadedAt,
                u.FullName as PrescribedByName,
                ISNULL(p.IsUploadedByNurse, 0) as IsUploadedByNurse
            FROM Prescriptions p
            INNER JOIN Users u ON p.PrescribedBy = u.UserId
            WHERE p.PatientId = @PatientId
            ORDER BY p.PrescriptionDate DESC",
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
                            Notes = reader.GetString(3),
                            UploadedAt = reader.GetString(4),
                            PrescribedByName = reader.GetString(5),
                            IsUploadedByNurse = reader.GetBoolean(6)
                        });
                    }
                }
            }
            return prescriptions;
        }

        private List<dynamic> GetPatientTestReports(int patientId)
        {
            var reports = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT tr.ReportId, tr.ReportName,
                   FORMAT(tr.ReportDate, 'dd-MMM-yyyy') as ReportDate,
                   tr.ReportFile, tr.Notes,
                   FORMAT(tr.UploadedAt, 'dd-MMM-yyyy HH:mm') as UploadedAt,
                   u.FullName as UploadedByName,
                   tr.IsUploadedByNurse
            FROM TestReports tr
            INNER JOIN Users u ON tr.UploadedBy = u.UserId
            WHERE tr.PatientId = @PatientId
            ORDER BY tr.ReportDate DESC",
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
                            ReportDate = reader.GetString(2),
                            ReportFile = reader.GetString(3),
                            Notes = reader.GetString(4),
                            UploadedAt = reader.GetString(5),
                            UploadedByName = reader.GetString(6),
                            IsUploadedByNurse = reader.GetBoolean(7)
                        });
                    }
                }
            }
            return reports;
        }

        private List<dynamic> GetPatientDiseases(int patientId)
        {
            var diseases = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT pdh.HistoryId, d.DiseaseName, d.Category,
                   FORMAT(pdh.DiagnosedDate, 'dd-MMM-yyyy') as DiagnosedDate,
                   pdh.Status, pdh.Notes,
                   u.FullName as DoctorName
            FROM PatientDiseaseHistory pdh
            INNER JOIN Diseases d ON pdh.DiseaseId = d.DiseaseId
            LEFT JOIN Users u ON pdh.DiagnosedByDoctor = u.UserId
            WHERE pdh.PatientId = @PatientId
            ORDER BY pdh.DiagnosedDate DESC",
                    connection);
                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        diseases.Add(new
                        {
                            HistoryId = reader.GetInt32(0),
                            DiseaseName = reader.GetString(1),
                            Category = reader.GetString(2),
                            DiagnosedDate = reader.GetString(3),
                            Status = reader.GetString(4),
                            Notes = reader.GetString(5),
                            DoctorName = reader.GetString(6)
                        });
                    }
                }
            }
            return diseases;
        }

        private List<dynamic> GetAllAppointments()
        {
            var appointments = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT a.AppointmentId, a.AppointmentDate, a.AppointmentTime, a.Status, a.Reason,
                           p.FullName as PatientName, d.FullName as DoctorName
                    FROM Appointments a
                    INNER JOIN Users p ON a.PatientId = p.UserId
                    INNER JOIN Users d ON a.DoctorId = d.UserId
                    ORDER BY a.AppointmentDate DESC, a.AppointmentTime DESC",
                    connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new
                        {
                            AppointmentId = reader.GetInt32(0),
                            AppointmentDate = reader.GetDateTime(1).ToString("dd-MMM-yyyy"),
                            AppointmentTime = reader.GetTimeSpan(2).ToString(@"hh\:mm"),
                            Status = reader.GetString(3),
                            Reason = reader.GetString(4),
                            PatientName = reader.GetString(5),
                            DoctorName = reader.GetString(6)
                        });
                    }
                }
            }
            return appointments;
        }
    }
}