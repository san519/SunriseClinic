using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SunriseClinic.Models;
using System.Data;

namespace SunriseClinic.Controllers
{
    public class ComplaintController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public ComplaintController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        // GET: /Complaint/Index (Admin view)
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (userType != "Admin")
            {
                TempData["ErrorMessage"] = "Admin access required";
                return RedirectToAction("Login", "Account");
            }

            var complaints = GetAllComplaints();
            ViewBag.Complaints = complaints;

            return View();
        }

        // GET: /Complaint/Search
        [HttpGet]
        public JsonResult Search(string query)
        {
            try
            {
                var complaints = SearchComplaints(query);
                return Json(new { success = true, data = complaints });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search error: {ex.Message}");
                return Json(new { success = false, message = "Search failed" });
            }
        }

        private List<dynamic> SearchComplaints(string query)
        {
            var complaints = new List<dynamic>();

            if (string.IsNullOrWhiteSpace(query))
                return GetAllComplaints();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
            SELECT 
                c.ComplaintId, 
                c.VisitorName, 
                c.VisitorEmail, 
                c.VisitorPhone, 
                c.Subject, 
                c.Description, 
                c.ComplaintDate, 
                c.IsResolved, 
                c.IsImportant, 
                c.AdminNotes,
                c.ResolvedDate,
                u.UserId,
                u.FullName AS PatientName,
                u.Email AS PatientEmail,
                u.UserType,
                dbo.GenerateDisplayId(u.UserId, u.UserType) AS DisplayId
            FROM Complaints c
            LEFT JOIN Users u ON c.PatientId = u.UserId
            WHERE 
                c.VisitorName LIKE @Query OR
                c.VisitorEmail LIKE @Query OR
                c.VisitorPhone LIKE @Query OR
                c.Subject LIKE @Query OR
                c.Description LIKE @Query OR
                u.FullName LIKE @Query OR
                u.Email LIKE @Query OR
                dbo.GenerateDisplayId(u.UserId, u.UserType) LIKE @Query
            ORDER BY c.ComplaintDate DESC",
                    connection);

                cmd.Parameters.AddWithValue("@Query", $"%{query}%");

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var userId = reader.IsDBNull(11) ? (int?)null : reader.GetInt32(11);
                        var userType = reader.IsDBNull(14) ? null : reader.GetString(14);
                        var displayId = reader.IsDBNull(15) ? null : reader.GetString(15);
                        bool isLoggedInUser = userId.HasValue;

                        string userDisplayName = "Visitor";

                        if (isLoggedInUser)
                        {
                            var fullName = reader.IsDBNull(12) ? null : reader.GetString(12);
                            if (!string.IsNullOrEmpty(displayId) && !string.IsNullOrEmpty(fullName))
                            {
                                userDisplayName = $"{fullName} ({displayId})";
                            }
                            else if (!string.IsNullOrEmpty(fullName))
                            {
                                userDisplayName = fullName;
                            }
                        }

                        complaints.Add(new
                        {
                            ComplaintId = reader.GetInt32(0),
                            VisitorName = reader.GetString(1),
                            VisitorEmail = reader.GetString(2),
                            VisitorPhone = reader.GetString(3),
                            Subject = reader.GetString(4),
                            Description = reader.GetString(5),
                            ComplaintDate = reader.GetDateTime(6),
                            IsResolved = reader.GetBoolean(7),
                            IsImportant = reader.GetBoolean(8),
                            AdminNotes = reader.IsDBNull(9) ? "" : reader.GetString(9),
                            ResolvedDate = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10),
                            PatientName = userDisplayName,
                            PatientEmail = reader.IsDBNull(13) ? "" : reader.GetString(13),
                            UserType = userType,
                            DisplayId = displayId,
                            IsLoggedInUser = isLoggedInUser
                        });
                    }
                }
            }

            return complaints;
        }

        // GET: /Complaint/GetLoggedInUserInfo
        [HttpGet]
        public JsonResult GetLoggedInUserInfo()
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                var userType = HttpContext.Session.GetString("UserType");

                // Check if user is logged in
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return Json(new
                    {
                        success = false,
                        isLoggedIn = false,
                        message = "Not logged in"
                    });
                }

                // Get user info from database
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var query = @"
                SELECT FullName, Email, PhoneNumber, UserType
                FROM Users 
                WHERE UserId = @UserId AND IsActive = 1";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userIdInt);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return Json(new
                                {
                                    success = true,
                                    isLoggedIn = true,
                                    name = reader["FullName"].ToString(),
                                    email = reader["Email"].ToString(),
                                    phone = reader["PhoneNumber"]?.ToString() ?? "",
                                    userType = reader["UserType"].ToString()
                                });
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = false,
                    isLoggedIn = false,
                    message = "User not found"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetLoggedInUserInfo error: {ex.Message}");
                return Json(new
                {
                    success = false,
                    isLoggedIn = false,
                    message = "Error retrieving user information"
                });
            }
        }

        // GET: /Complaint/GetUserInfo (Ajax call)
        [HttpGet]
        public JsonResult GetUserInfo()
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                var userType = HttpContext.Session.GetString("UserType");

                if (userType == "Patient" && int.TryParse(userId, out int patientId))
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        using (var cmd = new SqlCommand("sp_GetUserInfoForComplaint", connection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@UserId", patientId);

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    return Json(new
                                    {
                                        success = true,
                                        name = reader["FullName"].ToString(),
                                        email = reader["Email"].ToString(),
                                        phone = reader["PhoneNumber"].ToString(),
                                        userType = reader["UserType"].ToString()
                                    });
                                }
                            }
                        }
                    }
                }

                return Json(new { success = false, message = "Not logged in as patient" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetUserInfo error: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Complaint/Submit (Auto-important for ALL complaints)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Submit([FromBody] ComplaintSubmissionModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Name) ||
                    string.IsNullOrEmpty(model.Email) ||
                    string.IsNullOrEmpty(model.Phone) ||
                    string.IsNullOrEmpty(model.Subject) ||
                    string.IsNullOrEmpty(model.Description))
                {
                    return Json(new { success = false, message = "All fields are required" });
                }

                int? patientId = null;
                string? patientName = null;
                bool isLoggedInUser = false;
                string userType = "Visitor";

                // Check if user is logged in
                var sessionUserId = HttpContext.Session.GetString("UserId");
                var sessionUserType = HttpContext.Session.GetString("UserType");

                if (!string.IsNullOrEmpty(sessionUserId) && int.TryParse(sessionUserId, out int id))
                {
                    patientId = id;
                    isLoggedInUser = true;
                    userType = sessionUserType ?? "User";

                    // Get user name
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        var cmd = new SqlCommand("SELECT FullName FROM Users WHERE UserId = @UserId", connection);
                        cmd.Parameters.AddWithValue("@UserId", id);
                        var result = cmd.ExecuteScalar();
                        patientName = result?.ToString();
                    }
                }

                // Insert complaint with auto-important for ALL complaints
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var cmd = new SqlCommand(@"
            INSERT INTO Complaints (PatientId, VisitorName, VisitorEmail, VisitorPhone, 
                                   Subject, Description, ComplaintDate, IsResolved, IsImportant)
            OUTPUT INSERTED.ComplaintId
            VALUES (@PatientId, @VisitorName, @VisitorEmail, @VisitorPhone, 
                    @Subject, @Description, GETDATE(), 0, @IsImportant)",
                        connection);

                    cmd.Parameters.AddWithValue("@PatientId", patientId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@VisitorName", model.Name.Trim());
                    cmd.Parameters.AddWithValue("@VisitorEmail", model.Email.Trim());
                    cmd.Parameters.AddWithValue("@VisitorPhone", model.Phone.Trim());
                    cmd.Parameters.AddWithValue("@Subject", model.Subject);
                    cmd.Parameters.AddWithValue("@Description", model.Description.Trim());

                    // ✅ CHANGE: Auto-important for ALL complaints (both visitors and logged-in users)
                    cmd.Parameters.AddWithValue("@IsImportant", true); // Always true for new complaints

                    int complaintId = (int)cmd.ExecuteScalar();

                    // Create notification for admins
                    CreateAdminNotification(
                        $"New {model.Subject} received",
                        $"From: {model.Name} ({model.Email}) - {(isLoggedInUser ? $"{userType} User" : "Visitor")}",
                        complaintId
                    );

                    Console.WriteLine($"✅ Complaint #{complaintId} submitted by {model.Name} - AutoImportant: TRUE");
                }

                return Json(new
                {
                    success = true,
                    message = "Thank you for your feedback! We'll review it soon.",
                    userType = userType
                });
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error: {sqlEx.Message}");
                return Json(new
                {
                    success = false,
                    message = "Database error. Please try again."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Submit error: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Failed to submit feedback. Please try again."
                });
            }
        }

        // POST: /Complaint/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateStatus(int complaintId, bool isResolved, string adminNotes = null)
        {
            try
            {
                var userType = HttpContext.Session.GetString("UserType");
                if (userType != "Admin")
                    return Json(new { success = false, message = "Admin access required" });

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var cmd = new SqlCommand(@"
                        UPDATE Complaints 
                        SET IsResolved = @IsResolved, 
                            AdminNotes = @AdminNotes,
                            ResolvedDate = CASE WHEN @IsResolved = 1 THEN GETDATE() ELSE NULL END,
                            UpdatedAt = GETDATE()
                        WHERE ComplaintId = @ComplaintId",
                        connection);

                    cmd.Parameters.AddWithValue("@ComplaintId", complaintId);
                    cmd.Parameters.AddWithValue("@IsResolved", isResolved);
                    cmd.Parameters.AddWithValue("@AdminNotes", adminNotes ?? (object)DBNull.Value);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        string status = isResolved ? "resolved" : "reopened";
                        return Json(new
                        {
                            success = true,
                            message = $"Complaint {status} successfully"
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Complaint not found"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateStatus error: {ex.Message}");
                return Json(new { success = false, message = "Failed to update status" });
            }
        }

        // POST: /Complaint/ToggleImportant - SIMPLE WORKING VERSION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ToggleImportant([FromForm] int complaintId, [FromForm] bool isImportant)
        {
            try
            {
                Console.WriteLine($"ToggleImportant called: ID={complaintId}, IsImportant={isImportant}");

                var userType = HttpContext.Session.GetString("UserType");
                if (userType != "Admin")
                {
                    return Json(new
                    {
                        success = false,
                        message = "Admin access required"
                    });
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var cmd = new SqlCommand(@"
                UPDATE Complaints 
                SET IsImportant = @IsImportant,
                    UpdatedAt = GETDATE()
                WHERE ComplaintId = @ComplaintId",
                        connection);

                    cmd.Parameters.AddWithValue("@ComplaintId", complaintId);
                    cmd.Parameters.AddWithValue("@IsImportant", isImportant);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        string action = isImportant ? "marked as important" : "removed from important";
                        return Json(new
                        {
                            success = true,
                            message = $"Complaint {action} successfully"
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Complaint not found"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ToggleImportant error: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = $"Failed to update importance: {ex.Message}"
                });
            }
        }

        // Model for toggle important
        public class ToggleImportantRequest
        {
            public int ComplaintId { get; set; }
            public bool IsImportant { get; set; }
        }

        // GET: /Complaint/Details/{id}
        [HttpGet]
        public IActionResult Details(int id)
        {
            try
            {
                var complaint = GetComplaintById(id);
                if (complaint == null)
                    return Content("<div class='alert alert-danger'>Complaint not found</div>");

                return PartialView("_ComplaintDetails", complaint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Details error: {ex.Message}");
                return Content($"<div class='alert alert-danger'>Error: {ex.Message}</div>");
            }
        }

        // ==================== HELPER METHODS ====================

        private List<dynamic> GetAllComplaints()
        {
            var complaints = new List<dynamic>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Updated query to get Display ID
                    var cmd = new SqlCommand(@"
                SELECT 
                    c.ComplaintId, 
                    c.VisitorName, 
                    c.VisitorEmail, 
                    c.VisitorPhone, 
                    c.Subject, 
                    c.Description, 
                    c.ComplaintDate, 
                    c.IsResolved, 
                    c.IsImportant, 
                    c.AdminNotes,
                    c.ResolvedDate,
                    u.UserId,
                    u.FullName AS PatientName,
                    u.Email AS PatientEmail,
                    u.UserType,
                    dbo.GenerateDisplayId(u.UserId, u.UserType) AS DisplayId
                FROM Complaints c
                LEFT JOIN Users u ON c.PatientId = u.UserId
                ORDER BY c.ComplaintDate DESC",
                        connection);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var userId = reader.IsDBNull(11) ? (int?)null : reader.GetInt32(11);
                            var userType = reader.IsDBNull(14) ? null : reader.GetString(14);
                            var displayId = reader.IsDBNull(15) ? null : reader.GetString(15);

                            string userDisplayName = "Visitor";

                            if (!reader.IsDBNull(12)) // If PatientName exists
                            {
                                var fullName = reader.GetString(12);
                                if (!string.IsNullOrEmpty(displayId) && !string.IsNullOrEmpty(fullName))
                                {
                                    userDisplayName = $"{fullName} ({displayId})";
                                }
                                else if (!string.IsNullOrEmpty(fullName))
                                {
                                    userDisplayName = fullName;
                                }
                            }

                            complaints.Add(new
                            {
                                ComplaintId = reader.GetInt32(0),
                                VisitorName = reader.GetString(1),
                                VisitorEmail = reader.GetString(2),
                                VisitorPhone = reader.GetString(3),
                                Subject = reader.GetString(4),
                                Description = reader.GetString(5),
                                ComplaintDate = reader.GetDateTime(6),
                                IsResolved = reader.GetBoolean(7),
                                IsImportant = reader.GetBoolean(8),
                                AdminNotes = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                ResolvedDate = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10),
                                PatientName = userDisplayName,
                                PatientEmail = reader.IsDBNull(13) ? "" : reader.GetString(13),
                                UserType = userType,
                                DisplayId = displayId,
                                IsLoggedInUser = userId.HasValue
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllComplaints error: {ex.Message}");
            }

            return complaints;
        }

        private dynamic GetComplaintById(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var cmd = new SqlCommand(@"
                SELECT 
                    c.ComplaintId, 
                    c.VisitorName, 
                    c.VisitorEmail, 
                    c.VisitorPhone, 
                    c.Subject, 
                    c.Description, 
                    c.ComplaintDate, 
                    c.IsResolved, 
                    c.IsImportant, 
                    c.AdminNotes,
                    c.ResolvedDate,
                    u.UserId,
                    u.FullName AS PatientName,
                    u.Email AS PatientEmail,
                    u.PhoneNumber AS PatientPhone,
                    u.UserType,
                    dbo.GenerateDisplayId(u.UserId, u.UserType) AS DisplayId
                FROM Complaints c
                LEFT JOIN Users u ON c.PatientId = u.UserId
                WHERE c.ComplaintId = @ComplaintId",
                        connection);

                    cmd.Parameters.AddWithValue("@ComplaintId", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var userId = reader.IsDBNull(11) ? (int?)null : reader.GetInt32(11);
                            var userType = reader.IsDBNull(15) ? null : reader.GetString(15);
                            var displayId = reader.IsDBNull(16) ? null : reader.GetString(16);

                            string userDisplayName = "Visitor";

                            if (!reader.IsDBNull(12))
                            {
                                var fullName = reader.GetString(12);
                                if (!string.IsNullOrEmpty(displayId) && !string.IsNullOrEmpty(fullName))
                                {
                                    userDisplayName = $"{fullName} ({displayId})";
                                }
                                else if (!string.IsNullOrEmpty(fullName))
                                {
                                    userDisplayName = fullName;
                                }
                            }

                            return new
                            {
                                ComplaintId = reader.GetInt32(0),
                                VisitorName = reader.GetString(1),
                                VisitorEmail = reader.GetString(2),
                                VisitorPhone = reader.GetString(3),
                                Subject = reader.GetString(4),
                                Description = reader.GetString(5),
                                ComplaintDate = reader.GetDateTime(6),
                                IsResolved = reader.GetBoolean(7),
                                IsImportant = reader.GetBoolean(8),
                                AdminNotes = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                ResolvedDate = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10),
                                PatientId = userId,
                                PatientName = userDisplayName,
                                PatientEmail = reader.IsDBNull(13) ? "" : reader.GetString(13),
                                PatientPhone = reader.IsDBNull(14) ? "" : reader.GetString(14),
                                UserType = userType,
                                DisplayId = displayId,
                                IsLoggedInUser = userId.HasValue
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetComplaintById error: {ex.Message}");
            }

            return null;
        }

        private void CreateAdminNotification(string title, string message, int? relatedId = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Get all admin user IDs
                    var getAdminsCmd = new SqlCommand(@"
                        SELECT UserId FROM Users 
                        WHERE UserType = 'Admin' AND IsActive = 1", connection);

                    using (var reader = getAdminsCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var adminId = reader.GetInt32(0);

                            // Insert notification for each admin
                            using (var insertCmd = new SqlCommand(@"
                                INSERT INTO Notifications (UserId, Title, Message, IsRead, CreatedAt, RelatedId, NotificationType)
                                VALUES (@UserId, @Title, @Message, 0, GETDATE(), @RelatedId, 'Complaint')",
                                connection))
                            {
                                insertCmd.Parameters.AddWithValue("@UserId", adminId);
                                insertCmd.Parameters.AddWithValue("@Title", title);
                                insertCmd.Parameters.AddWithValue("@Message", message);
                                insertCmd.Parameters.AddWithValue("@RelatedId", relatedId ?? (object)DBNull.Value);

                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateAdminNotification error: {ex.Message}");
            }
        }
    }

    // ==================== MODEL CLASSES ====================

    public class ComplaintSubmissionModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
    }
}