using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SunriseClinic.Models;
using System.Data;
using System.Globalization;

namespace SunriseClinic.Controllers
{
    public class AppointmentManagementController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public AppointmentManagementController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Check if user has permission to manage appointments
        private bool CanManageAppointments()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            // Nurse, Admin, Doctor can manage appointments
            var allowedTypes = new[] { "Nurse", "Admin", "Doctor" };
            return !string.IsNullOrEmpty(userId) && allowedTypes.Contains(userType);
        }

        // GET current user info
        private (int UserId, string UserType, string UserName) GetCurrentUser()
        {
            var userId = int.Parse(HttpContext.Session.GetString("UserId"));
            var userType = HttpContext.Session.GetString("UserType");
            var userName = HttpContext.Session.GetString("UserName") ?? "User";
            return (userId, userType, userName);
        }

        // GET: /AppointmentManagement/Index - Main dashboard
        public IActionResult Index(string filter = "pending", string search = "")
        {
            if (!CanManageAppointments())
                return RedirectToAction("Login", "Account");

            var user = GetCurrentUser();
            ViewBag.UserType = user.UserType;
            ViewBag.UserName = user.UserName;
            ViewBag.CurrentFilter = filter;
            ViewBag.SearchQuery = search;

            // Get counts for stats
            var stats = GetAppointmentStats();
            ViewBag.Stats = stats;

            // Nurse এর জন্য sidebar stats সেট করুন
            if (user.UserType == "Nurse")
            {
                var sidebarStats = GetNurseSidebarStats(user.UserId);
                ViewBag.NurseSidebarStats = sidebarStats;
            }

            // Load appointments with filter and search
            try
            {
                var appointments = LoadAppointmentsWithFilter(filter, search);
                ViewBag.Appointments = appointments;
                ViewBag.AppointmentCount = appointments.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading appointments: {ex.Message}");
                ViewBag.Appointments = new List<dynamic>();
                ViewBag.AppointmentCount = 0;
            }

            return View();
        }

        // GET: /AppointmentManagement/Manage/{id} - Single appointment management page
        public IActionResult Manage(int id)
        {
            if (!CanManageAppointments())
                return RedirectToAction("Login", "Account");

            var user = GetCurrentUser();
            ViewBag.UserType = user.UserType;
            ViewBag.UserName = user.UserName;

            try
            {
                var appointment = GetAppointmentDetailsForManage(id);
                if (appointment == null)
                {
                    TempData["ErrorMessage"] = "Appointment not found";
                    return RedirectToAction("Index");
                }

                // Get available time slots for this doctor on the selected date
                var availableTimeSlots = GetAvailableTimeSlotsForDoctor(appointment.DoctorId, appointment.AppointmentDate);
                ViewBag.AvailableTimeSlots = availableTimeSlots;

                return View(appointment);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading appointment details";
                Console.WriteLine($"Error in Manage: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        // POST: /AppointmentManagement/UpdateStatus - Update appointment status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int appointmentId, string status, string reason = "")
        {
            if (!CanManageAppointments())
            {
                TempData["ErrorMessage"] = "Access denied";
                return RedirectToAction("Login", "Account");
            }

            var user = GetCurrentUser();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // 1. প্রথমে appointment details পেতে পার্টেন্টের জন্য
                    var getCmd = new SqlCommand(
                        @"SELECT PatientId, Status FROM Appointments 
                  WHERE AppointmentId = @AppointmentId",
                        connection);
                    getCmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

                    using (var reader = getCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int patientId = reader.GetInt32(0);
                            string currentStatus = reader.GetString(1);

                            reader.Close();

                            // 2. Validate status transition
                            if (!IsValidStatusTransition(currentStatus, status))
                            {
                                TempData["ErrorMessage"] = $"Cannot change status from {currentStatus} to {status}";
                                return RedirectToAction("Manage", new { id = appointmentId });
                            }

                            // 3. Update status with proper parameter handling
                            var updateCmd = new SqlCommand(@"
                        UPDATE Appointments 
                        SET Status = @Status,
                            UpdatedAt = GETDATE(),
                            UpdatedBy = @UpdatedBy,
                            Symptoms = CASE WHEN @Status = 'Rejected' 
                                      THEN ISNULL(Symptoms, '') + ' [Rejected: ' + ISNULL(@Reason, '') + ']'
                                      ELSE Symptoms END
                        WHERE AppointmentId = @AppointmentId",
                                connection);

                            updateCmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
                            updateCmd.Parameters.AddWithValue("@Status", status);
                            updateCmd.Parameters.AddWithValue("@UpdatedBy", user.UserId);
                            updateCmd.Parameters.AddWithValue("@Reason", reason ?? "");

                            int rowsAffected = updateCmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                // Create notification for patient
                                string notificationMessage = $"Your appointment has been {status.ToLower()}";
                                if (!string.IsNullOrEmpty(reason) && status == "Rejected")
                                {
                                    notificationMessage += $". Reason: {reason}";
                                }

                                CreateNotification(patientId,
                                    $"Appointment {status}",
                                    notificationMessage,
                                    appointmentId);

                                TempData["SuccessMessage"] = $"Appointment successfully {status.ToLower()}!";
                            }
                            else
                            {
                                TempData["ErrorMessage"] = "Failed to update appointment status";
                            }
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Appointment not found";
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                TempData["ErrorMessage"] = $"Database error: {sqlEx.Message}";
                Console.WriteLine($"SQL Error in UpdateStatus: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating appointment status";
                Console.WriteLine($"UpdateStatus error: {ex.Message}");
            }

            return RedirectToAction("Manage", new { id = appointmentId });
        }

        // Helper method to validate status transition
        private bool IsValidStatusTransition(string currentStatus, string newStatus)
        {
            var validTransitions = new Dictionary<string, List<string>>
    {
        { "Pending", new List<string> { "Approved", "Rejected", "Cancelled" } },
        { "Approved", new List<string> { "Completed", "Cancelled" } },
        { "Rejected", new List<string> { } }, // Once rejected, no further changes
        { "Cancelled", new List<string> { } }, // Once cancelled, no further changes
        { "Completed", new List<string> { } }  // Once completed, no further changes
    };

            if (validTransitions.ContainsKey(currentStatus))
            {
                return validTransitions[currentStatus].Contains(newStatus);
            }

            return false;
        }

        // POST: /AppointmentManagement/UpdateDetails - Update appointment date/time/details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateDetails(int appointmentId, DateTime appointmentDate,
            string appointmentTime, string reason, string symptoms, bool isEmergency)
        {
            if (!CanManageAppointments())
            {
                TempData["ErrorMessage"] = "Access denied";
                return RedirectToAction("Login", "Account");
            }

            var user = GetCurrentUser();

            try
            {
                // Parse time
                if (!TimeSpan.TryParse(appointmentTime, out TimeSpan timeSpan))
                {
                    TempData["ErrorMessage"] = "Invalid time format";
                    return RedirectToAction("Manage", new { id = appointmentId });
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Get doctor ID first
                    var getDoctorCmd = new SqlCommand(
                        "SELECT DoctorId FROM Appointments WHERE AppointmentId = @AppointmentId",
                        connection);
                    getDoctorCmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
                    int doctorId = (int)getDoctorCmd.ExecuteScalar();

                    // Check if time slot is available
                    var checkCmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM Appointments 
                        WHERE DoctorId = @DoctorId
                        AND AppointmentDate = @AppointmentDate 
                        AND AppointmentTime = @AppointmentTime 
                        AND Status IN ('Pending', 'Approved')
                        AND AppointmentId != @AppointmentId",
                        connection);

                    checkCmd.Parameters.AddWithValue("@DoctorId", doctorId);
                    checkCmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
                    checkCmd.Parameters.AddWithValue("@AppointmentDate", appointmentDate);
                    checkCmd.Parameters.AddWithValue("@AppointmentTime", timeSpan);

                    int count = (int)checkCmd.ExecuteScalar();

                    if (count > 0)
                    {
                        TempData["ErrorMessage"] = "This time slot is already booked. Please choose another time.";
                        return RedirectToAction("Manage", new { id = appointmentId });
                    }

                    // Get patient ID for notification
                    var getPatientCmd = new SqlCommand(
                        "SELECT PatientId FROM Appointments WHERE AppointmentId = @AppointmentId",
                        connection);
                    getPatientCmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
                    int patientId = (int)getPatientCmd.ExecuteScalar();

                    // Update appointment details
                    var updateCmd = new SqlCommand(@"
                        UPDATE Appointments 
                        SET AppointmentDate = @AppointmentDate,
                            AppointmentTime = @AppointmentTime,
                            Reason = @Reason,
                            Symptoms = @Symptoms,
                            IsEmergency = @IsEmergency,
                            UpdatedAt = GETDATE(),
                            UpdatedBy = @UpdatedBy
                        WHERE AppointmentId = @AppointmentId",
                        connection);

                    updateCmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
                    updateCmd.Parameters.AddWithValue("@AppointmentDate", appointmentDate);
                    updateCmd.Parameters.AddWithValue("@AppointmentTime", timeSpan);
                    updateCmd.Parameters.AddWithValue("@Reason", reason ?? "");
                    updateCmd.Parameters.AddWithValue("@Symptoms", symptoms ?? "");
                    updateCmd.Parameters.AddWithValue("@IsEmergency", isEmergency);
                    updateCmd.Parameters.AddWithValue("@UpdatedBy", user.UserId);

                    int rowsAffected = updateCmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Create notification
                        CreateNotification(patientId,
                            "Appointment Updated",
                            $"Your appointment has been rescheduled to {appointmentDate:dd-MMM-yyyy} at {appointmentTime}",
                            appointmentId);

                        TempData["SuccessMessage"] = "Appointment details updated successfully!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update appointment details";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating appointment details";
                Console.WriteLine($"UpdateDetails error: {ex.Message}");
            }

            return RedirectToAction("Manage", new { id = appointmentId });
        }

        // GET: /AppointmentManagement/GetTimeSlots - AJAX call for available time slots
        [HttpGet]
        public IActionResult GetTimeSlots(int doctorId, string date)
        {
            if (!CanManageAppointments())
                return Json(new { success = false, message = "Access denied" });

            try
            {
                var timeSlots = GetAvailableTimeSlotsForDoctor(doctorId, DateTime.Parse(date));
                return Json(new { success = true, timeSlots });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private List<dynamic> LoadAppointmentsWithFilter(string filter, string search)
        {
            var appointments = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string baseQuery = @"
            SELECT 
                a.AppointmentId,
                a.AppointmentDate,
                CONVERT(VARCHAR(5), a.AppointmentTime, 108) as AppointmentTime,
                a.Status,
                a.Reason,
                ISNULL(a.Symptoms, '') as Symptoms,
                a.IsEmergency,
                a.CreatedAt,
                p.FullName as PatientName,
                p.PhoneNumber as PatientPhone,
                p.Email as PatientEmail,
                d.FullName as DoctorName,
                doc.Specialization,
                doc.ConsultationFee,
                u2.FullName as UpdatedByName
            FROM Appointments a
            INNER JOIN Users p ON a.PatientId = p.UserId
            INNER JOIN Users d ON a.DoctorId = d.UserId
            INNER JOIN Doctors doc ON a.DoctorId = doc.DoctorId
            LEFT JOIN Users u2 ON a.UpdatedBy = u2.UserId
            WHERE 1=1";

                var conditions = new List<string>();
                var parameters = new List<SqlParameter>();

                // Apply filter
                switch (filter.ToLower())
                {
                    case "pending":
                        conditions.Add("a.Status = 'Pending'");
                        conditions.Add("a.AppointmentDate >= CAST(GETDATE() AS DATE)");
                        break;

                    case "today":
                        conditions.Add("a.AppointmentDate = CAST(GETDATE() AS DATE)");
                        break;

                    case "emergency":
                        conditions.Add("a.IsEmergency = 1");
                        conditions.Add("a.Status IN ('Pending', 'Approved')");
                        conditions.Add("a.AppointmentDate >= CAST(GETDATE() AS DATE)");
                        break;

                    case "approved":
                        conditions.Add("a.Status = 'Approved'");
                        break;

                    case "rejected":
                        conditions.Add("a.Status = 'Rejected'");  // ✅ Rejected ফিল্টার যোগ করুন
                        break;

                    case "cancelled":
                        conditions.Add("a.Status = 'Cancelled'");
                        break;

                    case "completed":
                        conditions.Add("a.Status = 'Completed'");
                        break;

                    case "all":
                        // No additional filter
                        break;
                }

                // Apply search
                if (!string.IsNullOrEmpty(search))
                {
                    // Remove "AP" prefix if user types it
                    string cleanSearch = search.ToUpper().Replace("AP", "").Trim();

                    // Try to parse as number
                    bool isNumeric = int.TryParse(cleanSearch, out int appointmentId);

                    if (isNumeric)
                    {
                        conditions.Add("a.AppointmentId = @AppointmentId");
                        parameters.Add(new SqlParameter("@AppointmentId", appointmentId));
                    }
                    else
                    {
                        conditions.Add(@"
                    (p.FullName LIKE @Search 
                    OR d.FullName LIKE @Search 
                    OR a.Reason LIKE @Search 
                    OR p.PhoneNumber LIKE @Search 
                    OR p.Email LIKE @Search
                    OR CONCAT('AP', RIGHT('000000' + CAST(a.AppointmentId AS VARCHAR(10)), 6)) LIKE @FormattedSearch)");

                        parameters.Add(new SqlParameter("@Search", $"%{search}%"));
                        parameters.Add(new SqlParameter("@FormattedSearch", $"%{search}%"));
                    }
                }

                // Build query
                if (conditions.Count > 0)
                {
                    baseQuery += " AND " + string.Join(" AND ", conditions);
                }

                baseQuery += " ORDER BY a.AppointmentDate DESC, a.AppointmentTime DESC";

                var cmd = new SqlCommand(baseQuery, connection);
                foreach (var param in parameters)
                {
                    cmd.Parameters.Add(param);
                }

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appointments.Add(new
                        {
                            AppointmentId = reader.GetInt32(0),
                            AppointmentDate = reader.GetDateTime(1),
                            AppointmentTime = reader.GetString(2),
                            Status = reader.GetString(3),
                            Reason = reader.GetString(4),
                            Symptoms = reader.GetString(5),
                            IsEmergency = reader.GetBoolean(6),
                            CreatedAt = reader.GetDateTime(7),
                            PatientName = reader.GetString(8),
                            PatientPhone = reader.GetString(9),
                            PatientEmail = reader.GetString(10),
                            DoctorName = reader.GetString(11),
                            Specialization = reader.GetString(12),
                            ConsultationFee = reader.GetDecimal(13),
                            UpdatedByName = reader.IsDBNull(14) ? null : reader.GetString(14),
                            AppointmentNumber = "AP" + reader.GetInt32(0).ToString("D6")
                        });
                    }
                }
            }

            return appointments;
        }

        private AppointmentManageViewModel GetAppointmentDetailsForManage(int appointmentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // প্রথমে ডিবাগ করার জন্য শুধু appointment data পড়ুন
                var debugCmd = new SqlCommand(
                    "SELECT AppointmentId, AppointmentDate, AppointmentTime, Status FROM Appointments WHERE AppointmentId = @AppointmentId",
                    connection);
                debugCmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

                using (var debugReader = debugCmd.ExecuteReader())
                {
                    if (debugReader.Read())
                    {
                        Console.WriteLine($"DEBUG - Appointment ID: {debugReader.GetInt32(0)}");
                        Console.WriteLine($"DEBUG - Appointment Date: {debugReader.GetDateTime(1)}");

                        // TimeSpan হিসেবে পড়ার চেষ্টা করুন
                        if (!debugReader.IsDBNull(2))
                        {
                            try
                            {
                                var timeSpan = debugReader.GetTimeSpan(2);
                                Console.WriteLine($"DEBUG - Appointment Time (TimeSpan): {timeSpan}");
                                Console.WriteLine($"DEBUG - Appointment Time (ToString): {timeSpan.ToString(@"hh\:mm")}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"DEBUG - Error reading TimeSpan: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("DEBUG - AppointmentTime is NULL");
                        }

                        Console.WriteLine($"DEBUG - Status: {debugReader.GetString(3)}");
                    }
                    debugReader.Close();
                }

                // এখন পুরো query
                var cmd = new SqlCommand(@"
        SELECT 
            a.AppointmentId,
            a.PatientId,
            a.DoctorId,
            a.AppointmentDate,
            a.AppointmentTime,
            a.Status,
            a.Reason,
            ISNULL(a.Symptoms, '') as Symptoms,
            a.IsEmergency,
            a.CreatedAt,
            p.FullName as PatientName,
            p.PhoneNumber as PatientPhone,
            p.Email as PatientEmail,
            p.DateOfBirth as PatientDOB,
            p.Gender as PatientGender,
            ISNULL(pat.BloodGroup, '') as BloodGroup,
            ISNULL(pat.EmergencyContact, '') as EmergencyContact,
            ISNULL(pat.Height, 0) as Height,
            ISNULL(pat.Weight, 0) as Weight,
            d.FullName as DoctorName,
            doc.Specialization,
            doc.Qualification,
            doc.ConsultationFee,
            doc.AvailableDays,
            doc.AvailableTime,
            doc.ExperienceYears
        FROM Appointments a
        INNER JOIN Users p ON a.PatientId = p.UserId
        LEFT JOIN Patients pat ON p.UserId = pat.PatientId
        INNER JOIN Users d ON a.DoctorId = d.UserId
        INNER JOIN Doctors doc ON a.DoctorId = doc.DoctorId
        WHERE a.AppointmentId = @AppointmentId",
                    connection);

                cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        TimeSpan appointmentTime;

                        // প্রথমে TimeSpan হিসেবে পড়ার চেষ্টা করুন
                        if (!reader.IsDBNull(4))
                        {
                            try
                            {
                                appointmentTime = reader.GetTimeSpan(4);
                                Console.WriteLine($"SUCCESS - Got TimeSpan: {appointmentTime}");
                            }
                            catch
                            {
                                // যদি TimeSpan না পড়া যায়, তাহলে string হিসেবে নিয়ে convert করুন
                                var timeString = reader.GetString(4);
                                Console.WriteLine($"Trying to parse string: {timeString}");

                                if (TimeSpan.TryParse(timeString, out appointmentTime))
                                {
                                    Console.WriteLine($"PARSED - TimeSpan from string: {appointmentTime}");
                                }
                                else
                                {
                                    Console.WriteLine($"FAILED - Could not parse time string");
                                    appointmentTime = TimeSpan.Zero;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("AppointmentTime is NULL in database");
                            appointmentTime = TimeSpan.Zero;
                        }

                        return new AppointmentManageViewModel
                        {
                            AppointmentId = reader.GetInt32(0),
                            PatientId = reader.GetInt32(1),
                            DoctorId = reader.GetInt32(2),
                            AppointmentDate = reader.GetDateTime(3),
                            AppointmentTimeSpan = appointmentTime,
                            AppointmentTime = appointmentTime.ToString(@"hh\:mm"),
                            Status = reader.GetString(5),
                            Reason = reader.GetString(6),
                            Symptoms = reader.GetString(7),
                            IsEmergency = reader.GetBoolean(8),
                            CreatedAt = reader.GetDateTime(9),
                            PatientName = reader.GetString(10),
                            PatientPhone = reader.GetString(11),
                            PatientEmail = reader.GetString(12),
                            PatientDOB = reader.IsDBNull(13) ? (DateTime?)null : reader.GetDateTime(13),
                            PatientGender = reader.GetString(14),
                            BloodGroup = reader.GetString(15),
                            EmergencyContact = reader.GetString(16),
                            Height = reader.GetDecimal(17),
                            Weight = reader.GetDecimal(18),
                            DoctorName = reader.GetString(19),
                            DoctorSpecialization = reader.GetString(20),
                            Qualification = reader.GetString(21),
                            ConsultationFee = reader.GetDecimal(22),
                            AvailableDays = reader.GetString(23),
                            AvailableTime = reader.GetString(24),
                            ExperienceYears = reader.GetInt32(25)
                        };
                    }
                }
            }

            return null;
        }

        // AppointmentManagementController.cs - এই মেথডটি যোগ করুন
        private dynamic GetNurseSidebarStats(int nurseId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT 
                (SELECT COUNT(*) FROM Users WHERE UserType = 'Patient') as TotalPatients,
                (SELECT COUNT(*) FROM Appointments 
                 WHERE AppointmentDate = CAST(GETDATE() AS DATE)) as TodaysAppointments,
                (SELECT COUNT(*) FROM Appointments 
                 WHERE Status = 'Pending' 
                 AND AppointmentDate >= CAST(GETDATE() AS DATE)) as PendingAppointments",
                    connection);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            TotalPatients = reader.GetInt32(0),
                            TodaysAppointments = reader.GetInt32(1),
                            PendingAppointments = reader.GetInt32(2)
                        };
                    }
                }
            }
            return null;
        }

        private dynamic GetAppointmentStats()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            SELECT 
                (SELECT COUNT(*) FROM Appointments WHERE Status = 'Pending' AND AppointmentDate >= CAST(GETDATE() AS DATE)) as Pending,
                (SELECT COUNT(*) FROM Appointments WHERE IsEmergency = 1 AND Status IN ('Pending', 'Approved') AND AppointmentDate >= CAST(GETDATE() AS DATE)) as Emergency,
                (SELECT COUNT(*) FROM Appointments WHERE AppointmentDate = CAST(GETDATE() AS DATE)) as Today,
                (SELECT COUNT(*) FROM Appointments) as Total,
                (SELECT COUNT(*) FROM Appointments WHERE Status = 'Approved') as Approved,
                (SELECT COUNT(*) FROM Appointments WHERE Status = 'Rejected') as Rejected,
                (SELECT COUNT(*) FROM Appointments WHERE Status = 'Cancelled') as Cancelled,
                (SELECT COUNT(*) FROM Appointments WHERE Status = 'Completed') as Completed",
                    connection);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            Pending = reader.GetInt32(0),
                            Emergency = reader.GetInt32(1),
                            Today = reader.GetInt32(2),
                            Total = reader.GetInt32(3),
                            Approved = reader.GetInt32(4),
                            Rejected = reader.GetInt32(5),
                            Cancelled = reader.GetInt32(6),
                            Completed = reader.GetInt32(7)
                        };
                    }
                }
            }
            return new
            {
                Pending = 0,
                Emergency = 0,
                Today = 0,
                Total = 0,
                Approved = 0,
                Rejected = 0,
                Cancelled = 0,
                Completed = 0
            };
        }

        private List<string> GetAvailableTimeSlotsForDoctor(int doctorId, DateTime date)
        {
            // Generate time slots from 9 AM to 5 PM, 30 minutes interval
            var allTimeSlots = new List<string>();
            var startTime = new TimeSpan(9, 0, 0); // 9:00 AM
            var endTime = new TimeSpan(17, 0, 0);  // 5:00 PM

            while (startTime <= endTime)
            {
                allTimeSlots.Add(startTime.ToString(@"hh\:mm"));
                startTime = startTime.Add(TimeSpan.FromMinutes(30));
            }

            // Get booked time slots
            var bookedTimeSlots = new List<string>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT CONVERT(VARCHAR(5), AppointmentTime, 108) as BookedTime
                    FROM Appointments 
                    WHERE DoctorId = @DoctorId 
                    AND AppointmentDate = @AppointmentDate 
                    AND Status IN ('Pending', 'Approved')
                    AND AppointmentTime IS NOT NULL",
                    connection);

                cmd.Parameters.AddWithValue("@DoctorId", doctorId);
                cmd.Parameters.AddWithValue("@AppointmentDate", date);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bookedTimeSlots.Add(reader.GetString(0));
                    }
                }
            }

            // Filter out booked time slots
            return allTimeSlots.Where(slot => !bookedTimeSlots.Contains(slot)).ToList();
        }

        private void CreateNotification(int userId, string title, string message, int? relatedId = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(@"
                        INSERT INTO Notifications (UserId, Title, Message, IsRead, CreatedAt, RelatedId)
                        VALUES (@UserId, @Title, @Message, 0, GETDATE(), @RelatedId)",
                        connection);

                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Title", title);
                    cmd.Parameters.AddWithValue("@Message", message);
                    cmd.Parameters.AddWithValue("@RelatedId", relatedId ?? (object)DBNull.Value);

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