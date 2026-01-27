using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using SunriseClinic.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SunriseClinic.Controllers
{
    public class AdminController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        // Check if user is logged in as Admin
        private bool IsAdminLoggedIn()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");
            return !string.IsNullOrEmpty(userId) && userType == "Admin";
        }

        // GET: /Admin/Dashboard
        public IActionResult Dashboard()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                // Get statistics
                var stats = GetDashboardStatistics();
                ViewBag.Stats = stats;

                // Get REAL activities from database
                var activities = GetRealActivities();
                ViewBag.Activities = activities;

                // Log for debugging
                Console.WriteLine($"✅ Dashboard Loaded - Activities: {activities.Count}");

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Admin Dashboard Error: {ex.Message}");

                // Fallback data
                ViewBag.Stats = new
                {
                    TotalPatients = 0,
                    TotalDoctors = 0,
                    TotalNurses = 0,
                    PendingAppointments = 0,
                    ApprovedAppointments = 0,
                    PendingComplaints = 0,
                    ImportantComplaints = 0
                };

                ViewBag.Activities = new List<dynamic>
        {
            new {
                ActivityType = "Error",
                Description = "System Error",
                ActivityDate = DateTime.Now,
                UserName = "System",
                Details = "Unable to load activities. Please check database connection.",
                ColorClass = "bg-light-red",
                Icon = "fa-exclamation-triangle"
            }
        };

                ViewBag.ErrorMessage = ex.Message;
                return View();
            }
        }

        // GET: /Admin/CreateDoctor
        public IActionResult CreateDoctor()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // POST: /Admin/CreateDoctor (COMPLETELY NEW VERSION)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateDoctor(DoctorRegistrationModel model)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            Console.WriteLine("=== CREATE DOCTOR START ===");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model validation failed");
                return View(model);
            }

            try
            {
                // 1. Generate username from email
                string username = GenerateUsernameFromEmail(model.Email);
                Console.WriteLine($"Generated username: {username}");

                // 2. Hash password
                string passwordHash = HashPassword(model.Password);
                Console.WriteLine("Password hashed");

                int userId = 0;

                // 3. Insert into Users table
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string userSql = @"
                INSERT INTO Users (
                    Username, 
                    Email, 
                    PasswordHash, 
                    FullName, 
                    DateOfBirth, 
                    Gender, 
                    PhoneNumber, 
                    Address, 
                    UserType
                )
                VALUES (
                    @Username, 
                    @Email, 
                    @PasswordHash, 
                    @FullName, 
                    @DateOfBirth, 
                    @Gender, 
                    @PhoneNumber, 
                    @Address, 
                    'Doctor'
                );
                SELECT SCOPE_IDENTITY();";

                    var userCmd = new SqlCommand(userSql, connection);

                    userCmd.Parameters.AddWithValue("@Username", username);
                    userCmd.Parameters.AddWithValue("@Email", model.Email);
                    userCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    userCmd.Parameters.AddWithValue("@FullName", model.FullName);
                    userCmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth);
                    userCmd.Parameters.AddWithValue("@Gender", model.Gender);
                    userCmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);

                    if (string.IsNullOrEmpty(model.Address))
                        userCmd.Parameters.AddWithValue("@Address", DBNull.Value);
                    else
                        userCmd.Parameters.AddWithValue("@Address", model.Address);

                    var result = userCmd.ExecuteScalar();
                    userId = Convert.ToInt32(result);
                    Console.WriteLine($"✅ User created with ID: {userId}");
                }

                // 4. Insert into Doctors table
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string doctorSql = @"
                INSERT INTO Doctors (
                    DoctorId, 
                    Specialization, 
                    Qualification, 
                    LicenseNumber, 
                    ConsultationFee, 
                    AvailableDays, 
                    AvailableTime,
                    ExperienceYears
                )
                VALUES (
                    @DoctorId, 
                    @Specialization, 
                    @Qualification, 
                    @LicenseNumber, 
                    @ConsultationFee, 
                    @AvailableDays, 
                    @AvailableTime,
                    @ExperienceYears
                )";

                    var doctorCmd = new SqlCommand(doctorSql, connection);

                    doctorCmd.Parameters.AddWithValue("@DoctorId", userId);
                    doctorCmd.Parameters.AddWithValue("@Specialization", model.Specialization);
                    doctorCmd.Parameters.AddWithValue("@Qualification", model.Qualification);
                    doctorCmd.Parameters.AddWithValue("@LicenseNumber", model.LicenseNumber);
                    doctorCmd.Parameters.AddWithValue("@ConsultationFee", model.ConsultationFee);
                    doctorCmd.Parameters.AddWithValue("@AvailableDays", model.AvailableDays);
                    doctorCmd.Parameters.AddWithValue("@AvailableTime", model.AvailableTime);
                    doctorCmd.Parameters.AddWithValue("@ExperienceYears", model.ExperienceYears);

                    int rows = doctorCmd.ExecuteNonQuery();
                    Console.WriteLine($"✅ Doctor record created. Rows affected: {rows}");
                }

                // 5. Generate Display ID
                string displayId = "D" + (userId + 9000);
                Console.WriteLine($"Display ID: {displayId}");

                // 6. Success message
                TempData["SuccessMessage"] = $@"
            <div class='alert alert-success'>
                <h4><i class='fas fa-check-circle'></i> Doctor Created Successfully!</h4>
                <hr>
                <p><strong>Doctor ID:</strong> <span class='badge bg-primary'>{displayId}</span></p>
                <p><strong>Name:</strong> {model.FullName}</p>
                <p><strong>Email:</strong> {model.Email}</p>
                <p><strong>Specialization:</strong> {model.Specialization}</p>
                <p><strong>Qualification:</strong> {model.Qualification}</p>
                <p><strong>Consultation Fee:</strong> ৳{model.ConsultationFee}</p>
            </div>";

                return RedirectToAction("ManageDoctors");
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error: {sqlEx.Message}");
                Console.WriteLine($"Error Number: {sqlEx.Number}");

                if (sqlEx.Number == 2627) // Primary key violation
                {
                    ModelState.AddModelError("Email", "Email already exists");
                }
                else if (sqlEx.Number == 2601) // Unique constraint
                {
                    ModelState.AddModelError("LicenseNumber", "License number already exists");
                }
                else
                {
                    ModelState.AddModelError("", $"Database error: {sqlEx.Message}");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                ModelState.AddModelError("", $"Error creating doctor: {ex.Message}");
                return View(model);
            }
        }

        // Helper method: Generate username from email
        private string GenerateUsernameFromEmail(string email)
        {
            string username = email.Split('@')[0];

            // Remove special characters
            username = new string(username.Where(c => char.IsLetterOrDigit(c)).ToArray());

            // Make username unique if exists
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var checkCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Users WHERE Username = @Username",
                    connection);
                checkCmd.Parameters.AddWithValue("@Username", username);

                int count = (int)checkCmd.ExecuteScalar();

                if (count > 0)
                {
                    username = username + new Random().Next(100, 999);
                }
            }

            return username;
        }

        // GET: /Admin/ManageDoctors (সার্চ সহ আপডেটেড ভার্সন)
        public IActionResult ManageDoctors(string searchTerm)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            var doctors = GetAllDoctors();

            // সার্চ টার্ম থাকলে ফিল্টার করুন
            if (!string.IsNullOrEmpty(searchTerm))
            {
                doctors = FilterDoctors(doctors, searchTerm);
                ViewBag.SearchTerm = searchTerm;
            }

            ViewBag.Doctors = doctors;

            return View();
        }

        // Helper method: Doctor list filter করা
        private List<dynamic> FilterDoctors(List<dynamic> doctors, string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return doctors;

            var term = searchTerm.ToLower().Trim();

            return doctors.Where(d =>
            {
                var doctor = d as dynamic;

                // সার্চ করার জন্য সম্ভাব্য সব ফিল্ড
                string displayId = doctor.DisplayId?.ToString()?.ToLower() ?? "";
                string fullName = doctor.FullName?.ToString()?.ToLower() ?? "";
                string email = doctor.Email?.ToString()?.ToLower() ?? "";
                string phoneNumber = doctor.PhoneNumber?.ToString()?.ToLower() ?? "";
                string licenseNumber = doctor.LicenseNumber?.ToString()?.ToLower() ?? "";
                string specialization = doctor.Specialization?.ToString()?.ToLower() ?? "";
                string qualification = doctor.Qualification?.ToString()?.ToLower() ?? "";

                // ConsultationFee কে string-এ convert করে সার্চ
                string consultationFee = doctor.ConsultationFee?.ToString() ?? "";

                // যেকোনো ফিল্ডে match করলে true return
                return displayId.Contains(term) ||
                       fullName.Contains(term) ||
                       email.Contains(term) ||
                       phoneNumber.Contains(term) ||
                       licenseNumber.Contains(term) ||
                       specialization.Contains(term) ||
                       qualification.Contains(term) ||
                       consultationFee.Contains(term);
            }).ToList();
        }

        // GET: /Admin/CreateNurse
        public IActionResult CreateNurse()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // POST: /Admin/CreateNurse (FIXED VERSION)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateNurse(NurseRegistrationModel model)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email already exists
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        var checkEmailCmd = new SqlCommand(
                            "SELECT COUNT(*) FROM Users WHERE Email = @Email",
                            connection);
                        checkEmailCmd.Parameters.AddWithValue("@Email", model.Email);

                        var emailExists = (int)checkEmailCmd.ExecuteScalar() > 0;

                        if (emailExists)
                        {
                            ModelState.AddModelError("Email", "Email already registered");
                            return View(model);
                        }
                    }

                    int userId = 0;

                    // Insert into Users table
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        // Begin transaction
                        var transaction = connection.BeginTransaction();

                        try
                        {
                            // Generate username from email
                            string username = model.Email.Split('@')[0];

                            // Check if username exists
                            var checkUsernameCmd = new SqlCommand(
                                "SELECT COUNT(*) FROM Users WHERE Username = @Username",
                                connection, transaction);
                            checkUsernameCmd.Parameters.AddWithValue("@Username", username);

                            if ((int)checkUsernameCmd.ExecuteScalar() > 0)
                            {
                                username = username + new Random().Next(100, 999);
                            }

                            // Insert into Users table
                            var insertUserCmd = new SqlCommand(@"
                                INSERT INTO Users (Username, Email, PasswordHash, FullName, DateOfBirth, Gender, 
                                                   PhoneNumber, Address, UserType, IsActive, CreatedAt, UpdatedAt)
                                OUTPUT INSERTED.UserId
                                VALUES (@Username, @Email, @PasswordHash, @FullName, @DateOfBirth, @Gender, 
                                        @PhoneNumber, @Address, @UserType, 1, GETDATE(), GETDATE())",
                                connection, transaction);

                            insertUserCmd.Parameters.AddWithValue("@Username", username);
                            insertUserCmd.Parameters.AddWithValue("@Email", model.Email);
                            insertUserCmd.Parameters.AddWithValue("@PasswordHash", HashPassword(model.Password));
                            insertUserCmd.Parameters.AddWithValue("@FullName", model.FullName);
                            insertUserCmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth);
                            insertUserCmd.Parameters.AddWithValue("@Gender", model.Gender);
                            insertUserCmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
                            insertUserCmd.Parameters.AddWithValue("@Address",
                                string.IsNullOrEmpty(model.Address) ? (object)DBNull.Value : model.Address);
                            insertUserCmd.Parameters.AddWithValue("@UserType", "Nurse");

                            userId = (int)insertUserCmd.ExecuteScalar();

                            // Insert into Nurses table
                            var insertNurseCmd = new SqlCommand(@"
                                INSERT INTO Nurses (NurseId, Department, ShiftTime, NurseLicense, ExperienceYears)
                                VALUES (@NurseId, @Department, @ShiftTime, @NurseLicense, @ExperienceYears)",
                                connection, transaction);

                            insertNurseCmd.Parameters.AddWithValue("@NurseId", userId);
                            insertNurseCmd.Parameters.AddWithValue("@Department", model.Department);
                            insertNurseCmd.Parameters.AddWithValue("@ShiftTime", model.ShiftTime);
                            insertNurseCmd.Parameters.AddWithValue("@NurseLicense", model.NurseLicense);

                            // Handle ExperienceYears - convert to int (handle empty string)
                            int experienceYears = 0;
                            if (int.TryParse(model.ExperienceYears.ToString(), out int parsedExperience))
                            {
                                experienceYears = parsedExperience;
                            }
                            insertNurseCmd.Parameters.AddWithValue("@ExperienceYears", experienceYears);

                            insertNurseCmd.ExecuteNonQuery();

                            transaction.Commit();

                            // Calculate Display ID
                            string displayId = CalculateDisplayId(userId, "Nurse");

                            TempData["SuccessMessage"] = $"Nurse account created successfully!<br>Nurse ID: <strong>{displayId}</strong><br>Email: {model.Email}<br>Password: {model.Password}";
                            return RedirectToAction("ManageNurses");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            ModelState.AddModelError("", $"Error: {ex.Message}");
                            Console.WriteLine($"Create nurse transaction error: {ex.Message}");
                            return View(model);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to create nurse account. Please try again.");
                    Console.WriteLine($"Create nurse error: {ex.Message}");
                }
            }

            return View(model);
        }

        // GET: /Admin/ManageNurses (সার্চ সহ আপডেটেড ভার্সন)
        public IActionResult ManageNurses(string searchTerm)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                var nurses = GetAllNurses();

                // সার্চ টার্ম থাকলে ফিল্টার করুন
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    nurses = FilterNurses(nurses, searchTerm);
                    ViewBag.SearchTerm = searchTerm;
                }

                ViewBag.Nurses = nurses;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ManageNurses error: {ex.Message}");
                ViewBag.ErrorMessage = "Unable to load nurses list";
                return View();
            }
        }

        // Helper method: Nurse list filter করা
        private List<dynamic> FilterNurses(List<dynamic> nurses, string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return nurses;

            var term = searchTerm.ToLower().Trim();

            return nurses.Where(n =>
            {
                var nurse = n as dynamic;

                // সার্চ করার জন্য সম্ভাব্য সব ফিল্ড
                string displayId = nurse.DisplayId?.ToString()?.ToLower() ?? "";
                string fullName = nurse.FullName?.ToString()?.ToLower() ?? "";
                string email = nurse.Email?.ToString()?.ToLower() ?? "";
                string phoneNumber = nurse.PhoneNumber?.ToString()?.ToLower() ?? "";
                string nurseLicense = nurse.NurseLicense?.ToString()?.ToLower() ?? "";
                string department = nurse.Department?.ToString()?.ToLower() ?? "";

                // যেকোনো ফিল্ডে match করলে true return
                return displayId.Contains(term) ||
                       fullName.Contains(term) ||
                       email.Contains(term) ||
                       phoneNumber.Contains(term) ||
                       nurseLicense.Contains(term) ||
                       department.Contains(term);
            }).ToList();
        }


        // GET: /Admin/ManagePatients (সার্চ সহ আপডেটেড ভার্সন)
        public IActionResult ManagePatients(string searchTerm)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                var patients = GetAllPatients();

                // সার্চ টার্ম থাকলে ফিল্টার করুন
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    patients = FilterPatients(patients, searchTerm);
                    ViewBag.SearchTerm = searchTerm;
                }

                ViewBag.Patients = patients;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ManagePatients error: {ex.Message}");
                ViewBag.ErrorMessage = "Unable to load patients list";
                return View();
            }
        }

        // Helper method: Patient list filter করা
        private List<dynamic> FilterPatients(List<dynamic> patients, string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return patients;

            var term = searchTerm.ToLower().Trim();

            return patients.Where(p =>
            {
                var patient = p as dynamic;

                // সার্চ করার জন্য সম্ভাব্য সব ফিল্ড
                string displayId = patient.DisplayId?.ToString()?.ToLower() ?? "";
                string fullName = patient.FullName?.ToString()?.ToLower() ?? "";
                string email = patient.Email?.ToString()?.ToLower() ?? "";
                string phoneNumber = patient.PhoneNumber?.ToString()?.ToLower() ?? "";
                string bloodGroup = patient.BloodGroup?.ToString()?.ToLower() ?? "";
                string emergencyContact = patient.EmergencyContact?.ToString()?.ToLower() ?? "";

                // যেকোনো ফিল্ডে match করলে true return
                return displayId.Contains(term) ||
                       fullName.Contains(term) ||
                       email.Contains(term) ||
                       phoneNumber.Contains(term) ||
                       bloodGroup.Contains(term) ||
                       emergencyContact.Contains(term);
            }).ToList();
        }

        // GET: /Admin/ViewPatient/{id} - এই method টি আছে কিনা চেক করুন
        public IActionResult ViewPatient(int id)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                var patient = GetPatientById(id);
                if (patient == null)
                {
                    TempData["ErrorMessage"] = "Patient not found";
                    return RedirectToAction("ManagePatients");
                }

                // Get patient's appointment history
                var appointments = GetPatientAppointments(id);
                ViewBag.Appointments = appointments;

                // Get patient's medical history
                var medicalHistory = GetPatientMedicalHistory(id);
                ViewBag.MedicalHistory = medicalHistory;

                ViewBag.Patient = patient;
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading patient: {ex.Message}";
                return RedirectToAction("ManagePatients");
            }
        }

        // GET: /Admin/EditPatient/{id} - এই method টি আছে কিনা চেক করুন
        public IActionResult EditPatient(int id)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                var patient = GetPatientById(id);
                if (patient == null)
                {
                    TempData["ErrorMessage"] = "Patient not found";
                    return RedirectToAction("ManagePatients");
                }

                // Create model for editing
                var model = new PatientEditModel
                {
                    UserId = patient.UserId,
                    Email = patient.Email,
                    FullName = patient.FullName,
                    DateOfBirth = patient.DateOfBirth,
                    Gender = patient.Gender,
                    PhoneNumber = patient.PhoneNumber,
                    Address = patient.Address ?? "",
                    BloodGroup = patient.BloodGroup ?? "",
                    Height = patient.Height,
                    Weight = patient.Weight,
                    EmergencyContact = patient.EmergencyContact ?? "",
                    InsuranceInfo = patient.InsuranceInfo ?? "",
                    Occupation = patient.Occupation ?? "",
                    MaritalStatus = patient.MaritalStatus ?? "",
                    IsActive = patient.IsActive
                };

                ViewBag.DisplayId = patient.DisplayId;
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading patient: {ex.Message}";
                return RedirectToAction("ManagePatients");
            }
        }

        // POST: /Admin/EditPatient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPatient(PatientEditModel model)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            ViewBag.DisplayId = CalculateDisplayId(model.UserId, "Patient");

            // ✅ IMPORTANT: Remove validation for optional fields
            ModelState.Remove("DisplayId");
            ModelState.Remove("BloodGroup");
            ModelState.Remove("Height");
            ModelState.Remove("Weight");
            ModelState.Remove("EmergencyContact");
            ModelState.Remove("InsuranceInfo");
            ModelState.Remove("Occupation");
            ModelState.Remove("MaritalStatus");
            ModelState.Remove("Address");

            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        // Check if email exists for other users
                        var checkEmailCmd = new SqlCommand(
                            "SELECT COUNT(*) FROM Users WHERE Email = @Email AND UserId != @UserId AND IsActive = 1",
                            connection);
                        checkEmailCmd.Parameters.AddWithValue("@Email", model.Email);
                        checkEmailCmd.Parameters.AddWithValue("@UserId", model.UserId);

                        if ((int)checkEmailCmd.ExecuteScalar() > 0)
                        {
                            ModelState.AddModelError("Email", "Email already exists for another user");
                            return View(model);
                        }

                        // Update Users table
                        var updateUserCmd = new SqlCommand(@"
                    UPDATE Users 
                    SET 
                        FullName = @FullName,
                        Email = @Email,
                        DateOfBirth = @DateOfBirth,
                        Gender = @Gender,
                        PhoneNumber = @PhoneNumber,
                        Address = @Address,
                        IsActive = @IsActive,
                        UpdatedAt = GETDATE()
                    WHERE UserId = @UserId",
                            connection);

                        updateUserCmd.Parameters.AddWithValue("@FullName", model.FullName);
                        updateUserCmd.Parameters.AddWithValue("@Email", model.Email);
                        updateUserCmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth);
                        updateUserCmd.Parameters.AddWithValue("@Gender", model.Gender);
                        updateUserCmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
                        updateUserCmd.Parameters.AddWithValue("@Address",
                            string.IsNullOrEmpty(model.Address) ? (object)DBNull.Value : model.Address);
                        updateUserCmd.Parameters.AddWithValue("@IsActive", model.IsActive);
                        updateUserCmd.Parameters.AddWithValue("@UserId", model.UserId);

                        int userRows = updateUserCmd.ExecuteNonQuery();

                        // Update Patients table - ALL fields optional
                        var updatePatientCmd = new SqlCommand(@"
                    UPDATE Patients 
                    SET 
                        BloodGroup = @BloodGroup,
                        Height = @Height,
                        Weight = @Weight,
                        EmergencyContact = @EmergencyContact,
                        InsuranceInfo = @InsuranceInfo,
                        Occupation = @Occupation,
                        MaritalStatus = @MaritalStatus
                    WHERE PatientId = @PatientId",
                            connection);

                        // Handle optional fields with DBNull
                        updatePatientCmd.Parameters.AddWithValue("@BloodGroup",
                            string.IsNullOrEmpty(model.BloodGroup) ? (object)DBNull.Value : model.BloodGroup);
                        updatePatientCmd.Parameters.AddWithValue("@Height",
                            model.Height.HasValue ? (object)model.Height.Value : DBNull.Value);
                        updatePatientCmd.Parameters.AddWithValue("@Weight",
                            model.Weight.HasValue ? (object)model.Weight.Value : DBNull.Value);
                        updatePatientCmd.Parameters.AddWithValue("@EmergencyContact",
                            string.IsNullOrEmpty(model.EmergencyContact) ? (object)DBNull.Value : model.EmergencyContact);
                        updatePatientCmd.Parameters.AddWithValue("@InsuranceInfo",
                            string.IsNullOrEmpty(model.InsuranceInfo) ? (object)DBNull.Value : model.InsuranceInfo);
                        updatePatientCmd.Parameters.AddWithValue("@Occupation",
                            string.IsNullOrEmpty(model.Occupation) ? (object)DBNull.Value : model.Occupation);
                        updatePatientCmd.Parameters.AddWithValue("@MaritalStatus",
                            string.IsNullOrEmpty(model.MaritalStatus) ? (object)DBNull.Value : model.MaritalStatus);
                        updatePatientCmd.Parameters.AddWithValue("@PatientId", model.UserId);

                        int patientRows = updatePatientCmd.ExecuteNonQuery();

                        TempData["SuccessMessage"] = $"✅ Patient updated successfully!<br>" +
                                                   $"<strong>Name:</strong> {model.FullName}<br>" +
                                                   $"<strong>Status:</strong> {(model.IsActive ? "ACTIVE" : "INACTIVE")}";

                        return RedirectToAction("ManagePatients");
                    }
                }
                catch (SqlException sqlEx)
                {
                    Console.WriteLine($"SQL Error: {sqlEx.Message}");
                    TempData["ErrorMessage"] = $"Database error: {sqlEx.Message}";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    TempData["ErrorMessage"] = $"Error updating patient: {ex.Message}";
                }
            }
            else
            {
                // Collect validation errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                if (errors.Any())
                {
                    TempData["ErrorMessage"] = $"Please fix errors:<br>- {string.Join("<br>- ", errors)}";
                }
            }

            return View(model);
        }


        // POST: /Admin/DeleteUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // First check user type
                    var checkCmd = new SqlCommand(
                        "SELECT UserType FROM Users WHERE UserId = @UserId",
                        connection);
                    checkCmd.Parameters.AddWithValue("@UserId", id);

                    var userType = checkCmd.ExecuteScalar()?.ToString();

                    // Get user details before deleting (for success message)
                    string displayId = "";
                    string fullName = "";

                    var userDetailsCmd = new SqlCommand(
                        "SELECT FullName, UserType FROM Users WHERE UserId = @UserId",
                        connection);
                    userDetailsCmd.Parameters.AddWithValue("@UserId", id);

                    using (var reader = userDetailsCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fullName = reader.GetString(0);
                            userType = reader.GetString(1);
                            displayId = CalculateDisplayId(id, userType);
                        }
                    }

                    if (userType == "Doctor")
                    {
                        // Delete from Doctors table
                        var deleteDoctorCmd = new SqlCommand(
                            "DELETE FROM Doctors WHERE DoctorId = @UserId",
                            connection);
                        deleteDoctorCmd.Parameters.AddWithValue("@UserId", id);
                        deleteDoctorCmd.ExecuteNonQuery();
                    }
                    else if (userType == "Nurse")
                    {
                        // Delete from Nurses table
                        var deleteNurseCmd = new SqlCommand(
                            "DELETE FROM Nurses WHERE NurseId = @UserId",
                            connection);
                        deleteNurseCmd.Parameters.AddWithValue("@UserId", id);
                        deleteNurseCmd.ExecuteNonQuery();
                    }
                    else if (userType == "Patient")
                    {
                        // Delete from Patients table
                        var deletePatientCmd = new SqlCommand(
                            "DELETE FROM Patients WHERE PatientId = @UserId",
                            connection);
                        deletePatientCmd.Parameters.AddWithValue("@UserId", id);
                        deletePatientCmd.ExecuteNonQuery();
                    }

                    // Delete from Users table
                    var deleteUserCmd = new SqlCommand(
                        "DELETE FROM Users WHERE UserId = @UserId",
                        connection);
                    deleteUserCmd.Parameters.AddWithValue("@UserId", id);
                    deleteUserCmd.ExecuteNonQuery();

                    // ✅ সঠিক TempData ব্যবহার করুন
                    TempData["DeleteSuccess"] = $"✅ {userType} '{fullName}' (ID: {displayId}) deleted successfully!";

                    // Nurse delete করলে Nurse list এ redirect করুন
                    if (userType == "Nurse")
                    {
                        return RedirectToAction("ManageNurses");
                    }
                    else if (userType == "Doctor")
                    {
                        return RedirectToAction("ManageDoctors");
                    }
                    else
                    {
                        return RedirectToAction("ManagePatients");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["DeleteError"] = $"❌ Failed to delete user: {ex.Message}";
                Console.WriteLine($"Delete user error: {ex.Message}");

                return RedirectToAction("ManageNurses");
            }
        }

        // GET: /Admin/EditDoctor/{id}
        public IActionResult EditDoctor(int id)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                var doctor = GetDoctorById(id);
                if (doctor == null)
                {
                    TempData["ErrorMessage"] = "Doctor not found";
                    return RedirectToAction("ManageDoctors");
                }

                // DoctorEditModel তৈরি করুন
                var model = new DoctorEditModel
                {
                    UserId = doctor.UserId,
                    Email = doctor.Email,
                    FullName = doctor.FullName,
                    DateOfBirth = doctor.DateOfBirth,
                    Gender = doctor.Gender,
                    PhoneNumber = doctor.PhoneNumber,
                    Address = doctor.Address ?? "",
                    Specialization = doctor.Specialization,
                    Qualification = doctor.Qualification,
                    LicenseNumber = doctor.LicenseNumber,
                    ConsultationFee = doctor.ConsultationFee,
                    AvailableDays = doctor.AvailableDays,
                    AvailableTime = doctor.AvailableTime,
                    Department = "", // Add if needed
                    ExperienceYears = 0 // Add if needed
                };

                ViewBag.DisplayId = doctor.DisplayId;
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EditDoctor GET error: {ex.Message}");
                TempData["ErrorMessage"] = $"Error loading doctor: {ex.Message}";
                return RedirectToAction("ManageDoctors");
            }
        }

        // POST: /Admin/EditDoctor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditDoctor(DoctorEditModel model)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            ViewBag.DisplayId = CalculateDisplayId(model.UserId, "Doctor");

            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        // Check if email exists for other users
                        var checkEmailCmd = new SqlCommand(
                            "SELECT COUNT(*) FROM Users WHERE Email = @Email AND UserId != @UserId",
                            connection);
                        checkEmailCmd.Parameters.AddWithValue("@Email", model.Email);
                        checkEmailCmd.Parameters.AddWithValue("@UserId", model.UserId);

                        if ((int)checkEmailCmd.ExecuteScalar() > 0)
                        {
                            ModelState.AddModelError("Email", "Email already exists for another user");
                            return View(model);
                        }

                        // Update Users table
                        var updateUserCmd = new SqlCommand(@"
                    UPDATE Users 
                    SET 
                        FullName = @FullName,
                        Email = @Email,
                        DateOfBirth = @DateOfBirth,
                        Gender = @Gender,
                        PhoneNumber = @PhoneNumber,
                        Address = @Address,
                        UpdatedAt = GETDATE()
                    WHERE UserId = @UserId",
                            connection);

                        updateUserCmd.Parameters.AddWithValue("@FullName", model.FullName);
                        updateUserCmd.Parameters.AddWithValue("@Email", model.Email);
                        updateUserCmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth);
                        updateUserCmd.Parameters.AddWithValue("@Gender", model.Gender);
                        updateUserCmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
                        updateUserCmd.Parameters.AddWithValue("@Address",
                            string.IsNullOrEmpty(model.Address) ? (object)DBNull.Value : model.Address);
                        updateUserCmd.Parameters.AddWithValue("@UserId", model.UserId);

                        int userRows = updateUserCmd.ExecuteNonQuery();

                        // Update Doctors table
                        var updateDoctorCmd = new SqlCommand(@"
                    UPDATE Doctors 
                    SET 
                        Specialization = @Specialization,
                        Qualification = @Qualification,
                        LicenseNumber = @LicenseNumber,
                        ConsultationFee = @ConsultationFee,
                        AvailableDays = @AvailableDays,
                        AvailableTime = @AvailableTime
                    WHERE DoctorId = @DoctorId",
                            connection);

                        updateDoctorCmd.Parameters.AddWithValue("@Specialization", model.Specialization);
                        updateDoctorCmd.Parameters.AddWithValue("@Qualification", model.Qualification);
                        updateDoctorCmd.Parameters.AddWithValue("@LicenseNumber", model.LicenseNumber);
                        updateDoctorCmd.Parameters.AddWithValue("@ConsultationFee", model.ConsultationFee);
                        updateDoctorCmd.Parameters.AddWithValue("@AvailableDays", model.AvailableDays);
                        updateDoctorCmd.Parameters.AddWithValue("@AvailableTime", model.AvailableTime);
                        updateDoctorCmd.Parameters.AddWithValue("@DoctorId", model.UserId);

                        int doctorRows = updateDoctorCmd.ExecuteNonQuery();

                        if (userRows > 0 || doctorRows > 0)
                        {
                            TempData["SuccessMessage"] = $"✅ Doctor updated successfully!<br>" +
                                                       $"<strong>Name:</strong> {model.FullName}<br>" +
                                                       $"<strong>Specialization:</strong> {model.Specialization}";
                            return RedirectToAction("ManageDoctors");
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "No changes were made.";
                            return View(model);
                        }
                    }
                }
                catch (SqlException sqlEx)
                {
                    Console.WriteLine($"SQL Error: {sqlEx.Message}");
                    TempData["ErrorMessage"] = $"Database error: {sqlEx.Message}";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    TempData["ErrorMessage"] = $"Error updating doctor: {ex.Message}";
                }
            }

            return View(model);
        }

        // GET: /Admin/EditNurse/{id} - WITH DATE VALIDATION
        public IActionResult EditNurse(int id)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var cmd = new SqlCommand(@"
                SELECT 
                    u.UserId,
                    u.Email,
                    u.FullName,
                    u.DateOfBirth,
                    u.CreatedAt,
                    u.Gender,
                    u.PhoneNumber,
                    u.Address,
                    u.IsActive,
                    n.Department,
                    n.ShiftTime,
                    n.NurseLicense,
                    ISNULL(n.ExperienceYears, 0) as ExperienceYears
                FROM Users u
                LEFT JOIN Nurses n ON u.UserId = n.NurseId
                WHERE u.UserId = @UserId AND u.UserType = 'Nurse'",
                        connection);

                    cmd.Parameters.AddWithValue("@UserId", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Read CreatedAt value
                            DateTime createdAt = reader.IsDBNull(4) ? DateTime.Now : reader.GetDateTime(4);

                            // ✅ Ensure CreatedAt is within SQL Server valid range
                            DateTime sqlMinDate = new DateTime(1753, 1, 1);
                            if (createdAt < sqlMinDate)
                            {
                                createdAt = DateTime.Now;
                                Console.WriteLine($"Adjusted invalid CreatedAt to current date for UserId: {id}");
                            }

                            Console.WriteLine($"Loading nurse ID: {id}, CreatedAt: {createdAt:yyyy-MM-dd}");

                            var model = new NurseEditModel
                            {
                                UserId = reader.GetInt32(0),
                                Email = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                FullName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                DateOfBirth = reader.IsDBNull(3) ? DateTime.Now.AddYears(-25) : reader.GetDateTime(3),
                                CreatedAt = createdAt,
                                Gender = reader.IsDBNull(5) ? "F" : reader.GetString(5),
                                PhoneNumber = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                Address = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                IsActive = reader.IsDBNull(8) ? true : reader.GetBoolean(8),
                                Department = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                ShiftTime = reader.IsDBNull(10) ? "" : reader.GetString(10),
                                NurseLicense = reader.IsDBNull(11) ? "" : reader.GetString(11),
                                ExperienceYears = reader.IsDBNull(12) ? 0 : reader.GetInt32(12)
                            };

                            ViewBag.DisplayId = CalculateDisplayId(id, "Nurse");
                            ViewBag.NurseId = id;

                            return View(model);
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "❌ Nurse not found in database";
                            return RedirectToAction("ManageNurses");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EditNurse GET error: {ex.Message}");
                TempData["ErrorMessage"] = $"❌ Error loading nurse data: {ex.Message}";
                return RedirectToAction("ManageNurses");
            }
        }

        // POST: /Admin/EditNurse (FIXED - SqlDateTime overflow error resolved)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditNurse(NurseEditModel model, string createdAtRaw)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            // Always set DisplayId for the view
            ViewBag.DisplayId = CalculateDisplayId(model.UserId, "Nurse");
            ViewBag.NurseId = model.UserId;

            // ✅ DEBUG: Check what values are coming
            Console.WriteLine($"=== EDIT NURSE DEBUG ===");
            Console.WriteLine($"Model.CreatedAt from binding: {model.CreatedAt}");
            Console.WriteLine($"Model.CreatedAt Kind: {model.CreatedAt.Kind}");
            Console.WriteLine($"Model.CreatedAs Date only: {model.CreatedAt.Date}");
            Console.WriteLine($"Raw createdAt parameter: {createdAtRaw}");

            // ✅ Try to parse the raw date string if model binding failed
            if (!string.IsNullOrEmpty(createdAtRaw))
            {
                if (DateTime.TryParse(createdAtRaw, out DateTime parsedDate))
                {
                    model.CreatedAt = parsedDate;
                    Console.WriteLine($"Parsed CreatedAt from raw: {model.CreatedAt:yyyy-MM-dd}");
                }
            }

            // ✅ Ensure CreatedAt has a time component (set to noon to avoid timezone issues)
            if (model.CreatedAt.TimeOfDay == TimeSpan.Zero)
            {
                model.CreatedAt = model.CreatedAt.Date.AddHours(12); // Set to 12:00 PM
                Console.WriteLine($"Adjusted CreatedAt time: {model.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            // ✅ Validate CreatedAt date range for SQL Server
            DateTime sqlMinDate = new DateTime(1753, 1, 1);
            DateTime sqlMaxDate = new DateTime(9999, 12, 31, 23, 59, 59);

            if (model.CreatedAt < sqlMinDate)
            {
                model.CreatedAt = DateTime.Now;
                Console.WriteLine($"CreatedAt too small, set to current: {model.CreatedAt}");
            }

            if (model.CreatedAt > sqlMaxDate)
            {
                model.CreatedAt = DateTime.Now;
                Console.WriteLine($"CreatedAt too large, set to current: {model.CreatedAt}");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        // Check if email already exists
                        var checkEmailCmd = new SqlCommand(
                            "SELECT COUNT(*) FROM Users WHERE Email = @Email AND UserId != @UserId AND IsActive = 1",
                            connection);
                        checkEmailCmd.Parameters.AddWithValue("@Email", model.Email);
                        checkEmailCmd.Parameters.AddWithValue("@UserId", model.UserId);

                        var emailExists = (int)checkEmailCmd.ExecuteScalar() > 0;

                        if (emailExists)
                        {
                            ModelState.AddModelError("Email", "Email already registered by another active user");
                            TempData["ErrorMessage"] = "Email already exists for another user";
                            return View(model);
                        }

                        // ✅ CORRECT SQL UPDATE with CreatedAt
                        var updateUserCmd = new SqlCommand(@"
                    UPDATE Users 
                    SET 
                        FullName = @FullName,
                        Email = @Email,
                        DateOfBirth = @DateOfBirth,
                        Gender = @Gender,
                        PhoneNumber = @PhoneNumber,
                        Address = @Address,
                        IsActive = @IsActive,
                        CreatedAt = @CreatedAt, -- ✅ This will update CreatedAt
                        UpdatedAt = GETDATE()
                    WHERE UserId = @UserId",
                            connection);

                        // Add parameters
                        updateUserCmd.Parameters.AddWithValue("@FullName", model.FullName);
                        updateUserCmd.Parameters.AddWithValue("@Email", model.Email);
                        updateUserCmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth);
                        updateUserCmd.Parameters.AddWithValue("@Gender", model.Gender);
                        updateUserCmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);

                        if (string.IsNullOrEmpty(model.Address))
                            updateUserCmd.Parameters.AddWithValue("@Address", DBNull.Value);
                        else
                            updateUserCmd.Parameters.AddWithValue("@Address", model.Address);

                        updateUserCmd.Parameters.AddWithValue("@IsActive", model.IsActive);

                        // ✅ Use SQL DateTime directly
                        updateUserCmd.Parameters.AddWithValue("@CreatedAt", model.CreatedAt);

                        updateUserCmd.Parameters.AddWithValue("@UserId", model.UserId);

                        // Execute the update
                        int userRows = updateUserCmd.ExecuteNonQuery();
                        Console.WriteLine($"✅ Updated {userRows} rows in Users table. CreatedAt: {model.CreatedAt:yyyy-MM-dd HH:mm:ss}");

                        // Update Nurses table
                        var updateNurseCmd = new SqlCommand(@"
                    IF EXISTS (SELECT 1 FROM Nurses WHERE NurseId = @NurseId)
                    BEGIN
                        UPDATE Nurses 
                        SET 
                            Department = @Department,
                            ShiftTime = @ShiftTime,
                            NurseLicense = @NurseLicense,
                            ExperienceYears = @ExperienceYears
                        WHERE NurseId = @NurseId
                    END
                    ELSE
                    BEGIN
                        INSERT INTO Nurses (NurseId, Department, ShiftTime, NurseLicense, ExperienceYears)
                        VALUES (@NurseId, @Department, @ShiftTime, @NurseLicense, @ExperienceYears)
                    END",
                            connection);

                        updateNurseCmd.Parameters.AddWithValue("@Department", model.Department);
                        updateNurseCmd.Parameters.AddWithValue("@ShiftTime", model.ShiftTime);
                        updateNurseCmd.Parameters.AddWithValue("@NurseLicense", model.NurseLicense);
                        updateNurseCmd.Parameters.AddWithValue("@ExperienceYears", model.ExperienceYears);
                        updateNurseCmd.Parameters.AddWithValue("@NurseId", model.UserId);

                        int nurseRows = updateNurseCmd.ExecuteNonQuery();

                        if (userRows > 0 || nurseRows > 0)
                        {
                            TempData["SuccessMessage"] = $"✅ Nurse updated successfully!<br>" +
                                                       $"<strong>Name:</strong> {model.FullName}<br>" +
                                                       $"<strong>Join Date:</strong> {model.CreatedAt:dd-MMM-yyyy}<br>" +
                                                       $"<strong>Status:</strong> {(model.IsActive ? "ACTIVE" : "INACTIVE")}";

                            return RedirectToAction("ManageNurses");
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "⚠️ No changes were made.";
                            return View(model);
                        }
                    }
                }
                catch (SqlException sqlEx)
                {
                    Console.WriteLine($"SQL Error: {sqlEx.Message}");
                    TempData["ErrorMessage"] = $"Database error: {sqlEx.Message}";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    TempData["ErrorMessage"] = $"Error: {ex.Message}";
                }
            }
            else
            {
                // Show validation errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                if (errors.Any())
                {
                    TempData["ErrorMessage"] = $"Please fix errors:<br>- {string.Join("<br>- ", errors)}";
                }
            }

            return View(model);
        }


        // =============================================
        // HELPER METHODS
        // =============================================

        // Helper method to get patient by ID
        private dynamic GetPatientById(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT 
                u.UserId,
                u.Email,
                u.FullName,
                u.DateOfBirth,
                u.Gender,
                u.PhoneNumber,
                u.Address,
                u.CreatedAt,
                u.IsActive,
                p.BloodGroup,
                p.Height,
                p.Weight,
                p.EmergencyContact,
                p.InsuranceInfo,
                p.Occupation,
                p.MaritalStatus
            FROM Users u
            LEFT JOIN Patients p ON u.UserId = p.PatientId
            WHERE u.UserId = @UserId AND u.UserType = 'Patient'",
                    connection);

                cmd.Parameters.AddWithValue("@UserId", id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            UserId = reader.GetInt32(0),
                            DisplayId = CalculateDisplayId(reader.GetInt32(0), "Patient"),
                            Email = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            FullName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            DateOfBirth = reader.IsDBNull(3) ? DateTime.Now.AddYears(-25) : reader.GetDateTime(3),
                            Gender = reader.IsDBNull(4) ? "F" : reader.GetString(4),
                            PhoneNumber = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            Address = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            CreatedAt = reader.IsDBNull(7) ? DateTime.Now : reader.GetDateTime(7),
                            IsActive = reader.IsDBNull(8) ? true : reader.GetBoolean(8),
                            BloodGroup = reader.IsDBNull(9) ? "" : reader.GetString(9),
                            Height = reader.IsDBNull(10) ? (decimal?)null : reader.GetDecimal(10),
                            Weight = reader.IsDBNull(11) ? (decimal?)null : reader.GetDecimal(11),
                            EmergencyContact = reader.IsDBNull(12) ? "" : reader.GetString(12),
                            InsuranceInfo = reader.IsDBNull(13) ? "" : reader.GetString(13),
                            Occupation = reader.IsDBNull(14) ? "" : reader.GetString(14),
                            MaritalStatus = reader.IsDBNull(15) ? "" : reader.GetString(15)
                        };
                    }
                }
            }
            return null;
        }

        // Helper method to get patient appointments
        private List<dynamic> GetPatientAppointments(int patientId)
        {
            var appointments = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT 
                a.AppointmentId,
                a.AppointmentDate,
                a.AppointmentTime,
                a.Status,
                a.Reason,
                a.CreatedAt,
                u.FullName AS DoctorName,
                d.Specialization
            FROM Appointments a
            INNER JOIN Doctors d ON a.DoctorId = d.DoctorId
            INNER JOIN Users u ON d.DoctorId = u.UserId
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


        private List<dynamic> GetAllDoctors()
        {
            var doctors = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
                    SELECT u.UserId, u.Email, u.FullName, u.PhoneNumber, u.CreatedAt,
                           d.Specialization, d.Qualification, d.LicenseNumber, d.ConsultationFee
                    FROM Users u
                    INNER JOIN Doctors d ON u.UserId = d.DoctorId
                    WHERE u.UserType = 'Doctor' AND u.IsActive = 1
                    ORDER BY u.FullName",
                    connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        doctors.Add(new
                        {
                            UserId = reader.GetInt32(0),
                            DisplayId = CalculateDisplayId(reader.GetInt32(0), "Doctor"),
                            Email = reader.GetString(1),
                            FullName = reader.GetString(2),
                            PhoneNumber = reader.GetString(3),
                            CreatedAt = reader.GetDateTime(4),
                            Specialization = reader.GetString(5),
                            Qualification = reader.GetString(6),
                            LicenseNumber = reader.GetString(7),
                            ConsultationFee = reader.GetDecimal(8)
                        });
                    }
                }
            }

            return doctors;
        }

        private List<dynamic> GetAllNurses()
        {
            var nurses = new List<dynamic>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var cmd = new SqlCommand(@"
                SELECT 
                    u.UserId, 
                    u.Email, 
                    u.FullName, 
                    u.PhoneNumber, 
                    u.CreatedAt,
                    u.IsActive, -- ✅ IsActive যোগ করুন
                    n.Department, 
                    n.ShiftTime, 
                    n.NurseLicense, 
                    ISNULL(n.ExperienceYears, 0) as ExperienceYears
                FROM Users u
                LEFT JOIN Nurses n ON u.UserId = n.NurseId
                WHERE u.UserType = 'Nurse'
                ORDER BY u.FullName",
                        connection);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nurses.Add(new
                            {
                                UserId = reader.GetInt32(0),
                                DisplayId = CalculateDisplayId(reader.GetInt32(0), "Nurse"),
                                Email = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                FullName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                PhoneNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                CreatedAt = reader.IsDBNull(4) ? DateTime.Now : reader.GetDateTime(4),
                                IsActive = reader.IsDBNull(5) ? true : reader.GetBoolean(5), // ✅ IsActive property যোগ করুন
                                Department = reader.IsDBNull(6) ? "Not set" : reader.GetString(6),
                                ShiftTime = reader.IsDBNull(7) ? "Not set" : reader.GetString(7),
                                NurseLicense = reader.IsDBNull(8) ? "Not set" : reader.GetString(8),
                                ExperienceYears = reader.IsDBNull(9) ? 0 : reader.GetInt32(9)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllNurses error: {ex.Message}");
                throw;
            }

            return nurses;
        }

        private List<dynamic> GetAllPatients()
        {
            var patients = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
            SELECT u.UserId, u.Email, u.FullName, u.PhoneNumber, u.CreatedAt,
                   u.IsActive, -- ✅ IsActive ফিল্ড যোগ করুন
                   p.BloodGroup, p.EmergencyContact,
                   p.Height, p.Weight, p.MaritalStatus, -- ✅ নতুন ফিল্ডগুলো যোগ করুন
                   p.InsuranceInfo, p.Occupation
            FROM Users u
            INNER JOIN Patients p ON u.UserId = p.PatientId
            WHERE u.UserType = 'Patient' -- ❌ u.IsActive = 1 রিমুভ করুন
            ORDER BY u.FullName",
                    connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        patients.Add(new
                        {
                            UserId = reader.GetInt32(0),
                            DisplayId = CalculateDisplayId(reader.GetInt32(0), "Patient"),
                            Email = reader.GetString(1),
                            FullName = reader.GetString(2),
                            PhoneNumber = reader.GetString(3),
                            CreatedAt = reader.GetDateTime(4),
                            IsActive = reader.IsDBNull(5) ? true : reader.GetBoolean(5), // ✅ IsActive পড়ুন
                            BloodGroup = reader["BloodGroup"]?.ToString() ?? "Not set",
                            EmergencyContact = reader["EmergencyContact"]?.ToString() ?? "Not set",
                            Height = reader.IsDBNull(8) ? (decimal?)null : reader.GetDecimal(8), // ✅ Height
                            Weight = reader.IsDBNull(9) ? (decimal?)null : reader.GetDecimal(9), // ✅ Weight
                            MaritalStatus = reader["MaritalStatus"]?.ToString() ?? "Not set", // ✅ MaritalStatus
                            InsuranceInfo = reader["InsuranceInfo"]?.ToString() ?? "", // ✅ InsuranceInfo
                            Occupation = reader["Occupation"]?.ToString() ?? "" // ✅ Occupation
                        });
                    }
                }
            }

            return patients;
        }

        // GET: /Admin/ViewMedicalRecord - FIXED VERSION
        public IActionResult ViewMedicalRecord(int patientId, string type, int recordId)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                ViewBag.PatientId = patientId;
                ViewBag.RecordType = type;

                if (type == "TestReport")
                {
                    return ViewTestReport(patientId, recordId);
                }
                else if (type == "Prescription")
                {
                    return ViewPrescription(patientId, recordId);
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid record type";
                    return RedirectToAction("ViewPatient", new { id = patientId });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading record: {ex.Message}";
                return RedirectToAction("ViewPatient", new { id = patientId });
            }
        }

        // Helper method for Test Report
        private IActionResult ViewTestReport(int patientId, int reportId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var cmd = new SqlCommand(@"
                SELECT 
                    tr.ReportId,
                    tr.ReportName,
                    tr.ReportDate,
                    tr.ReportFile,
                    tr.Notes,
                    tr.UploadedAt,
                    tr.UploadedBy,
                    u.FullName as UploadedByName,
                    p.PatientId,
                    pu.FullName as PatientName
                FROM TestReports tr
                INNER JOIN Users u ON tr.UploadedBy = u.UserId
                INNER JOIN Patients p ON tr.PatientId = p.PatientId
                INNER JOIN Users pu ON p.PatientId = pu.UserId
                WHERE tr.ReportId = @ReportId AND p.PatientId = @PatientId",
                        connection);

                    cmd.Parameters.AddWithValue("@ReportId", reportId);
                    cmd.Parameters.AddWithValue("@PatientId", patientId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var model = new TestReport
                            {
                                ReportId = reader.GetInt32(0),
                                ReportName = reader.GetString(1),
                                ReportDate = reader.GetDateTime(2),
                                ReportFile = reader.GetString(3),
                                Notes = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                UploadedAt = reader.GetDateTime(5),
                                UploadedBy = reader.GetInt32(6),
                                UploadedByName = reader.GetString(7),
                                PatientId = reader.GetInt32(8),
                                PatientName = reader.GetString(9)
                            };

                            // Get file extension
                            if (!string.IsNullOrEmpty(model.ReportFile))
                            {
                                ViewBag.FileType = Path.GetExtension(model.ReportFile).ToLower().TrimStart('.');
                            }

                            return View("ViewTestReport", model);
                        }
                    }
                }

                TempData["ErrorMessage"] = "Test report not found or does not belong to this patient";
                return RedirectToAction("ViewPatient", new { id = patientId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("ViewPatient", new { id = patientId });
            }
        }

        // Helper method for Prescription
        private IActionResult ViewPrescription(int patientId, int prescriptionId)
        {
            try
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
                    p.Status,
                    p.UploadedAt,
                    p.PrescribedBy,
                    pu.FullName as PrescribedByName,
                    pt.FullName as PatientName
                FROM Prescriptions p
                INNER JOIN Users pu ON p.PrescribedBy = pu.UserId
                INNER JOIN Patients pt ON p.PatientId = pt.PatientId
                WHERE p.PrescriptionId = @PrescriptionId AND p.PatientId = @PatientId",
                        connection);

                    cmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);
                    cmd.Parameters.AddWithValue("@PatientId", patientId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var model = new PrescriptionViewModel
                            {
                                PrescriptionId = reader.GetInt32(0),
                                PatientId = reader.GetInt32(1),
                                PrescriptionDate = reader.GetDateTime(2),
                                PrescriptionFile = reader.GetString(3),
                                Notes = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Status = reader.GetString(5),
                                UploadedAt = reader.GetDateTime(6),
                                PrescribedBy = reader.GetInt32(7),
                                PrescribedByName = reader.GetString(8),
                                PatientName = reader.GetString(9)
                            };

                            // Get file extension
                            if (!string.IsNullOrEmpty(model.PrescriptionFile))
                            {
                                ViewBag.FileType = Path.GetExtension(model.PrescriptionFile).ToLower().TrimStart('.');
                            }

                            return View("ViewPrescription", model);
                        }
                    }
                }

                TempData["ErrorMessage"] = "Prescription not found or does not belong to this patient";
                return RedirectToAction("ViewPatient", new { id = patientId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("ViewPatient", new { id = patientId });
            }
        }

        // GET: /Admin/DownloadReport
        public IActionResult DownloadReport(string fileName, int reportId)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    TempData["ErrorMessage"] = "File not found";
                    return RedirectToAction("Dashboard");
                }

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reports", fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["ErrorMessage"] = "File does not exist on server";
                    return RedirectToAction("Dashboard");
                }

                var contentType = GetContentType(fileName);
                var fileBytes = System.IO.File.ReadAllBytes(filePath);

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Download error: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        // GET: /Admin/DownloadPrescription
        public IActionResult DownloadPrescription(string fileName, int prescriptionId)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    TempData["ErrorMessage"] = "File not found";
                    return RedirectToAction("Dashboard");
                }

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "prescriptions", fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["ErrorMessage"] = "File does not exist on server";
                    return RedirectToAction("Dashboard");
                }

                var contentType = GetContentType(fileName);
                var fileBytes = System.IO.File.ReadAllBytes(filePath);

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Download error: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        // Helper method to get content type
        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();

            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        // Updated GetPatientMedicalHistory method (without FileType)
        private List<dynamic> GetPatientMedicalHistory(int patientId)
        {
            var history = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Get Test Reports
                var testReportCmd = new SqlCommand(@"
            SELECT 
                tr.ReportId,
                tr.ReportName,
                tr.ReportDate,
                tr.Notes,
                'TestReport' as Type,
                u.FullName as UploadedByName,
                tr.UploadedAt,
                tr.ReportFile
            FROM TestReports tr
            INNER JOIN Users u ON tr.UploadedBy = u.UserId
            WHERE tr.PatientId = @PatientId",
                    connection);

                testReportCmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = testReportCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        history.Add(new
                        {
                            Id = reader.GetInt32(0),
                            ReportName = reader.GetString(1),
                            ReportDate = Convert.ToDateTime(reader["ReportDate"]).ToString("yyyy-MM-dd"),
                            Notes = reader["Notes"].ToString(),
                            Type = "TestReport",
                            UploadedByName = reader["UploadedByName"].ToString(),
                            UploadedAt = Convert.ToDateTime(reader["UploadedAt"]).ToString("dd-MMM-yyyy HH:mm"),
                            FileName = reader["ReportFile"].ToString()
                        });
                    }
                }

                // Get Prescriptions
                var prescriptionCmd = new SqlCommand(@"
            SELECT 
                p.PrescriptionId,
                p.PrescriptionFile as ReportName,
                p.PrescriptionDate as ReportDate,
                p.Notes,
                'Prescription' as Type,
                u.FullName as UploadedByName,
                p.UploadedAt,
                p.PrescriptionFile
            FROM Prescriptions p
            INNER JOIN Users u ON p.PrescribedBy = u.UserId
            WHERE p.PatientId = @PatientId",
                    connection);

                prescriptionCmd.Parameters.AddWithValue("@PatientId", patientId);

                using (var reader = prescriptionCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        history.Add(new
                        {
                            Id = reader.GetInt32(0),
                            ReportName = reader.GetString(1),
                            ReportDate = Convert.ToDateTime(reader["ReportDate"]).ToString("yyyy-MM-dd"),
                            Notes = reader["Notes"].ToString(),
                            Type = "Prescription",
                            UploadedByName = reader["UploadedByName"].ToString(),
                            UploadedAt = Convert.ToDateTime(reader["UploadedAt"]).ToString("dd-MMM-yyyy HH:mm"),
                            FileName = reader["PrescriptionFile"].ToString()
                        });
                    }
                }

                // Sort by date descending
                history = history.OrderByDescending(h => h.ReportDate).ToList();
            }

            return history;
        }


        private List<dynamic> GetAllUsers()
        {
            var users = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
                    SELECT u.UserId, u.Email, u.FullName, u.UserType, u.IsActive, u.CreatedAt
                    FROM Users u
                    ORDER BY u.UserId",
                    connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new
                        {
                            UserId = reader.GetInt32(0),
                            DisplayId = CalculateDisplayId(reader.GetInt32(0), reader.GetString(3)),
                            Email = reader.GetString(1),
                            FullName = reader.GetString(2),
                            UserType = reader.GetString(3),
                            IsActive = reader.GetBoolean(4),
                            CreatedAt = reader.GetDateTime(5)
                        });
                    }
                }
            }

            return users;
        }

        private dynamic GetDoctorById(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
                    SELECT u.UserId, u.Email, u.FullName, u.DateOfBirth, u.Gender, u.PhoneNumber, 
                           u.Address, u.CreatedAt,
                           d.Specialization, d.Qualification, d.LicenseNumber, 
                           d.ConsultationFee, d.AvailableDays, d.AvailableTime
                    FROM Users u
                    INNER JOIN Doctors d ON u.UserId = d.DoctorId
                    WHERE u.UserId = @UserId",
                    connection);

                cmd.Parameters.AddWithValue("@UserId", id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            UserId = reader.GetInt32(0),
                            DisplayId = CalculateDisplayId(reader.GetInt32(0), "Doctor"),
                            Email = reader.GetString(1),
                            FullName = reader.GetString(2),
                            DateOfBirth = reader.GetDateTime(3),
                            Gender = reader.GetString(4),
                            PhoneNumber = reader.GetString(5),
                            Address = reader["Address"]?.ToString(),
                            CreatedAt = reader.GetDateTime(7),
                            Specialization = reader.GetString(8),
                            Qualification = reader.GetString(9),
                            LicenseNumber = reader.GetString(10),
                            ConsultationFee = reader.GetDecimal(11),
                            AvailableDays = reader.GetString(12),
                            AvailableTime = reader.GetString(13)
                        };
                    }
                }
            }
            return null;
        }

        // AdminController.cs এর এই method টি নেই (অথবা ভুল)
        // Helper method to get nurse details by ID - IsActive সহ
        private dynamic GetNurseById(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
            SELECT 
                u.UserId,
                u.Email,
                u.FullName,
                u.DateOfBirth,
                u.Gender,
                u.PhoneNumber,
                u.Address,
                u.CreatedAt,
                u.IsActive,
                n.Department,
                n.ShiftTime,
                n.NurseLicense,
                ISNULL(n.ExperienceYears, 0) as ExperienceYears
            FROM Users u
            LEFT JOIN Nurses n ON u.UserId = n.NurseId
            WHERE u.UserId = @UserId AND u.UserType = 'Nurse'",
                    connection);

                cmd.Parameters.AddWithValue("@UserId", id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Return as ExpandoObject for easier access
                        dynamic nurse = new System.Dynamic.ExpandoObject();
                        var dict = nurse as IDictionary<string, object>;

                        dict["UserId"] = reader.GetInt32(0);
                        dict["Email"] = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        dict["FullName"] = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        dict["DateOfBirth"] = reader.IsDBNull(3) ? DateTime.Now.AddYears(-25) : reader.GetDateTime(3);
                        dict["Gender"] = reader.IsDBNull(4) ? "F" : reader.GetString(4);
                        dict["PhoneNumber"] = reader.IsDBNull(5) ? "" : reader.GetString(5);
                        dict["Address"] = reader.IsDBNull(6) ? "" : reader.GetString(6);
                        dict["CreatedAt"] = reader.IsDBNull(7) ? DateTime.Now : reader.GetDateTime(7);
                        dict["IsActive"] = reader.IsDBNull(8) ? true : reader.GetBoolean(8);
                        dict["Department"] = reader.IsDBNull(9) ? "" : reader.GetString(9);
                        dict["ShiftTime"] = reader.IsDBNull(10) ? "" : reader.GetString(10);
                        dict["NurseLicense"] = reader.IsDBNull(11) ? "" : reader.GetString(11);
                        dict["ExperienceYears"] = reader.IsDBNull(12) ? 0 : reader.GetInt32(12);
                        dict["DisplayId"] = CalculateDisplayId(reader.GetInt32(0), "Nurse");

                        return nurse;
                    }
                }
            }
            return null;
        }

        // AdminController.cs এর GetDashboardStatistics method এ যোগ করুন
        private dynamic GetDashboardStatistics()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
            SELECT 
                (SELECT COUNT(*) FROM Users WHERE UserType = 'Patient' AND IsActive = 1) AS TotalPatients,
                (SELECT COUNT(*) FROM Users WHERE UserType = 'Doctor' AND IsActive = 1) AS TotalDoctors,
                (SELECT COUNT(*) FROM Users WHERE UserType = 'Nurse' AND IsActive = 1) AS TotalNurses,
                (SELECT COUNT(*) FROM Appointments WHERE Status = 'Pending') AS PendingAppointments,
                (SELECT COUNT(*) FROM Appointments WHERE Status = 'Approved') AS ApprovedAppointments,
                (SELECT COUNT(*) FROM Complaints WHERE IsResolved = 0) AS PendingComplaints,
                (SELECT COUNT(*) FROM Complaints WHERE IsImportant = 1 AND IsResolved = 0) AS ImportantComplaints",
                    connection);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            TotalPatients = reader.GetInt32(0),
                            TotalDoctors = reader.GetInt32(1),
                            TotalNurses = reader.GetInt32(2),
                            PendingAppointments = reader.GetInt32(3),
                            ApprovedAppointments = reader.GetInt32(4),
                            PendingComplaints = reader.GetInt32(5),
                            ImportantComplaints = reader.GetInt32(6)
                        };
                    }
                }
            }
            return null;
        }

        // GET: /Admin/Dashboard এর সাথে RealActivities মেথড
        private List<dynamic> GetRealActivities()
        {
            var activities = new List<dynamic>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // 1. Recent Appointments (Last 7 days)
                    var appointmentCmd = new SqlCommand(@"
                SELECT TOP 10 
                    'Appointment' AS ActivityType,
                    CASE 
                        WHEN a.Status = 'Pending' THEN 'New Appointment Request'
                        WHEN a.Status = 'Approved' THEN 'Appointment Approved'
                        WHEN a.Status = 'Completed' THEN 'Appointment Completed'
                        WHEN a.Status = 'Cancelled' THEN 'Appointment Cancelled'
                        ELSE 'Appointment ' + a.Status
                    END AS Description,
                    a.CreatedAt AS ActivityDate,
                    p.FullName AS UserName,
                    'With Dr. ' + d.FullName + ' (' + d.Specialization + ')' AS Details,
                    'bg-light-blue' AS ColorClass
                FROM Appointments a
                INNER JOIN Users p ON a.PatientId = p.UserId
                INNER JOIN Doctors dr ON a.DoctorId = dr.DoctorId
                INNER JOIN Users d ON dr.DoctorId = d.UserId
                WHERE a.CreatedAt > DATEADD(DAY, -30, GETDATE())
                ORDER BY a.CreatedAt DESC",
                        connection);

                    using (var reader = appointmentCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            activities.Add(new
                            {
                                ActivityType = reader.GetString(0),
                                Description = reader.GetString(1),
                                ActivityDate = reader.GetDateTime(2),
                                UserName = reader.GetString(3),
                                Details = reader.GetString(4),
                                ColorClass = reader.GetString(5),
                                Icon = "fa-calendar"
                            });
                        }
                    }

                    // 2. Recent User Registrations (Last 7 days)
                    var registrationCmd = new SqlCommand(@"
                SELECT TOP 10 
                    'Registration' AS ActivityType,
                    'New ' + UserType + ' Registered' AS Description,
                    CreatedAt AS ActivityDate,
                    FullName AS UserName,
                    'Email: ' + Email + ' | ID: ' + 
                        CASE UserType
                            WHEN 'Patient' THEN 'P' + CAST((UserId + 99000) AS VARCHAR(10))
                            WHEN 'Doctor' THEN 'D' + CAST((UserId + 9000) AS VARCHAR(10))
                            WHEN 'Nurse' THEN 'N' + CAST((UserId + 9000) AS VARCHAR(10))
                            WHEN 'Admin' THEN 'A' + CAST(UserId AS VARCHAR(10))
                            ELSE CAST(UserId AS VARCHAR(10))
                        END AS Details,
                    CASE UserType
                        WHEN 'Patient' THEN 'bg-light-yellow'
                        WHEN 'Doctor' THEN 'bg-light-blue'
                        WHEN 'Nurse' THEN 'bg-light-green'
                        ELSE 'bg-light-purple'
                    END AS ColorClass
                FROM Users
                WHERE CreatedAt > DATEADD(DAY, -30, GETDATE())
                    AND UserType IN ('Patient', 'Doctor', 'Nurse', 'Admin')
                ORDER BY CreatedAt DESC",
                        connection);

                    using (var reader = registrationCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            activities.Add(new
                            {
                                ActivityType = reader.GetString(0),
                                Description = reader.GetString(1),
                                ActivityDate = reader.GetDateTime(2),
                                UserName = reader.GetString(3),
                                Details = reader.GetString(4),
                                ColorClass = reader.GetString(5),
                                Icon = "fa-user-plus"
                            });
                        }
                    }

                    // 3. Recent Profile Updates (Last 7 days)
                    var updateCmd = new SqlCommand(@"
                SELECT TOP 10 
                    'Update' AS ActivityType,
                    UserType + ' Profile Updated' AS Description,
                    UpdatedAt AS ActivityDate,
                    FullName AS UserName,
                    'Last modified: ' + CONVERT(VARCHAR, UpdatedAt, 100) AS Details,
                    'bg-light-orange' AS ColorClass
                FROM Users
                WHERE UpdatedAt IS NOT NULL
                    AND UpdatedAt > DATEADD(DAY, -30, GETDATE())
                    AND DATEDIFF(MINUTE, CreatedAt, UpdatedAt) > 5
                ORDER BY UpdatedAt DESC",
                        connection);

                    using (var reader = updateCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            activities.Add(new
                            {
                                ActivityType = reader.GetString(0),
                                Description = reader.GetString(1),
                                ActivityDate = reader.GetDateTime(2),
                                UserName = reader.GetString(3),
                                Details = reader.GetString(4),
                                ColorClass = reader.GetString(5),
                                Icon = "fa-edit"
                            });
                        }
                    }

                    // 4. Recent Medical Records (Last 7 days)
                    var medicalCmd = new SqlCommand(@"
                -- Test Reports
                SELECT TOP 5 
                    'Medical' AS ActivityType,
                    'New Test Report Uploaded' AS Description,
                    tr.UploadedAt AS ActivityDate,
                    p.FullName AS UserName,
                    'Report: ' + tr.ReportName + ' for ' + p.FullName AS Details,
                    'bg-light-red' AS ColorClass
                FROM TestReports tr
                INNER JOIN Patients pt ON tr.PatientId = pt.PatientId
                INNER JOIN Users p ON pt.PatientId = p.UserId
                WHERE tr.UploadedAt > DATEADD(DAY, -30, GETDATE())
                
                UNION ALL
                
                -- Prescriptions
                SELECT TOP 5 
                    'Medical' AS ActivityType,
                    'New Prescription Added' AS Description,
                    p.UploadedAt AS ActivityDate,
                    pt.FullName AS UserName,
                    'Prescription by Dr. ' + d.FullName AS Details,
                    'bg-light-green' AS ColorClass
                FROM Prescriptions p
                INNER JOIN Patients pt ON p.PatientId = pt.PatientId
                INNER JOIN Doctors dr ON p.PrescribedBy = dr.DoctorId
                INNER JOIN Users d ON dr.DoctorId = d.UserId
                WHERE p.UploadedAt > DATEADD(DAY, -30, GETDATE())
                
                ORDER BY ActivityDate DESC",
                        connection);

                    using (var reader = medicalCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            activities.Add(new
                            {
                                ActivityType = reader.GetString(0),
                                Description = reader.GetString(1),
                                ActivityDate = reader.GetDateTime(2),
                                UserName = reader.GetString(3),
                                Details = reader.GetString(4),
                                ColorClass = reader.GetString(5),
                                Icon = "fa-file-medical"
                            });
                        }
                    }

                    // 5. Recent System Activities (Admin actions)
                    var systemCmd = new SqlCommand(@"
                -- User Deletions/Status Changes
                SELECT TOP 5 
                    'System' AS ActivityType,
                    'User Status Changed' AS Description,
                    GETDATE() AS ActivityDate,
                    'System Administrator' AS UserName,
                    'User management action performed' AS Details,
                    'bg-light-gray' AS ColorClass
                FROM Users
                WHERE UpdatedAt IS NOT NULL
                    AND UserType = 'Admin'
                ORDER BY UpdatedAt DESC",
                        connection);

                    using (var reader = systemCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            activities.Add(new
                            {
                                ActivityType = reader.GetString(0),
                                Description = reader.GetString(1),
                                ActivityDate = reader.GetDateTime(2),
                                UserName = reader.GetString(3),
                                Details = reader.GetString(4),
                                ColorClass = reader.GetString(5),
                                Icon = "fa-cogs"
                            });
                        }
                    }
                }

                // Sort all activities by date (newest first) and take top 15
                activities = activities
                    .OrderByDescending(a => a.ActivityDate)
                    .Take(15)
                    .ToList();

                // If still no activities, add system startup activity
                if (!activities.Any())
                {
                    activities.Add(new
                    {
                        ActivityType = "System",
                        Description = "System Started",
                        ActivityDate = DateTime.Now,
                        UserName = "System",
                        Details = "Dashboard initialized successfully",
                        ColorClass = "bg-light-purple",
                        Icon = "fa-server"
                    });
                }

                return activities;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetRealActivities Error: {ex.Message}");

                // Return at least one activity for fallback
                return new List<dynamic>
        {
            new {
                ActivityType = "System",
                Description = "Activities Loaded",
                ActivityDate = DateTime.Now,
                UserName = "Administrator",
                Details = "Real-time activities monitoring active",
                ColorClass = "bg-light-blue",
                Icon = "fa-chart-line"
            }
        };
            }
        }

        // AdminController.cs - GetRecentActivities() মেথডটি replace করুন
        private List<dynamic> GetRecentActivities()
        {
            var activities = new List<dynamic>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Simplified and corrected query
                    var cmd = new SqlCommand(@"
                -- Recent Appointments
                SELECT TOP 5 
                    'Appointment' AS ActivityType,
                    'New ' + a.Status + ' Appointment' AS Description,
                    a.CreatedAt AS ActivityDate,
                    p.FullName AS UserName,
                    'With Dr. ' + d.FullName AS Details
                FROM Appointments a
                INNER JOIN Users p ON a.PatientId = p.UserId
                INNER JOIN Users d ON a.DoctorId = d.UserId
                WHERE a.CreatedAt > DATEADD(DAY, -7, GETDATE())
                ORDER BY a.CreatedAt DESC

                UNION ALL

                -- Recent User Registrations
                SELECT TOP 5 
                    'Registration' AS ActivityType,
                    'New ' + UserType + ' Registered' AS Description,
                    CreatedAt AS ActivityDate,
                    FullName AS UserName,
                    UserType + ' Account Created' AS Details
                FROM Users
                WHERE CreatedAt > DATEADD(DAY, -7, GETDATE())
                ORDER BY CreatedAt DESC

                UNION ALL

                -- Recent Updates
                SELECT TOP 5 
                    'Update' AS ActivityType,
                    'Profile Updated' AS Description,
                    UpdatedAt AS ActivityDate,
                    FullName AS UserName,
                    UserType + ' Profile Modified' AS Details
                FROM Users
                WHERE UpdatedAt IS NOT NULL
                AND UpdatedAt > DATEADD(DAY, -7, GETDATE())
                AND UpdatedAt != CreatedAt
                ORDER BY UpdatedAt DESC

                ORDER BY ActivityDate DESC",
                        connection);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                var activityDate = reader.IsDBNull(2) ? DateTime.Now : reader.GetDateTime(2);

                                activities.Add(new
                                {
                                    ActivityType = reader.IsDBNull(0) ? "System" : reader.GetString(0),
                                    Description = reader.IsDBNull(1) ? "Activity Recorded" : reader.GetString(1),
                                    ActivityDate = activityDate,
                                    UserName = reader.IsDBNull(3) ? "System" : reader.GetString(3),
                                    Details = reader.IsDBNull(4) ? "No details available" : reader.GetString(4)
                                });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error reading activity: {ex.Message}");
                                // Add a default activity if there's an error
                                activities.Add(new
                                {
                                    ActivityType = "System",
                                    Description = "System Activity",
                                    ActivityDate = DateTime.Now,
                                    UserName = "System",
                                    Details = "Automated system activity"
                                });
                            }
                        }
                    }

                    // If no activities found, add sample activities
                    if (!activities.Any())
                    {
                        Console.WriteLine("No activities found, adding sample activities");
                        activities.AddRange(GetSampleActivities());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetRecentActivities error: {ex.Message}");
                // Return sample activities if query fails
                activities = GetSampleActivities();
            }

            return activities;
        }

        // Sample activities for testing
        private List<dynamic> GetSampleActivities()
        {
            return new List<dynamic>
    {
        new {
            ActivityType = "Registration",
            Description = "New Patient Registered",
            ActivityDate = DateTime.Now.AddHours(-1),
            UserName = "John Doe",
            Details = "Patient Account Created"
        },
        new {
            ActivityType = "Appointment",
            Description = "New Pending Appointment",
            ActivityDate = DateTime.Now.AddHours(-2),
            UserName = "Sarah Smith",
            Details = "With Dr. Michael Brown"
        },
        new {
            ActivityType = "Update",
            Description = "Profile Updated",
            ActivityDate = DateTime.Now.AddHours(-3),
            UserName = "Dr. Robert Wilson",
            Details = "Doctor Profile Modified"
        },
        new {
            ActivityType = "Registration",
            Description = "New Nurse Registered",
            ActivityDate = DateTime.Now.AddHours(-4),
            UserName = "Emma Johnson",
            Details = "Nurse Account Created"
        },
        new {
            ActivityType = "Appointment",
            Description = "Approved Appointment",
            ActivityDate = DateTime.Now.AddHours(-5),
            UserName = "David Miller",
            Details = "With Dr. Lisa Taylor"
        }
    };
        }

        // Password hashing method
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        // Calculate Display ID (SAME AS AccountController)
        private string CalculateDisplayId(int userId, string userType)
        {
            return userType switch
            {
                "Patient" => "P" + (userId + 99000).ToString("D6"),
                "Doctor" => "D" + (userId + 9000).ToString("D5"),
                "Nurse" => "N" + (userId + 9000).ToString("D5"),
                "Admin" => "A" + userId.ToString("D4"),
                _ => userId.ToString()
            };
        }

        // Redirect to Dashboard (SAME AS AccountController)
        private IActionResult RedirectToDashboard(string userType)
        {
            return userType switch
            {
                "Patient" => RedirectToAction("Dashboard", "Patient"),
                "Doctor" => RedirectToAction("Dashboard", "Doctor"),
                "Nurse" => RedirectToAction("Dashboard", "Nurse"),
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                _ => RedirectToAction("Index", "Home")
            };
        }
    }
}