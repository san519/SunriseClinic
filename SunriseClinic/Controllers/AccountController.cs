using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SunriseClinic.Models;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SunriseClinic.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // =============================================
        // REGISTRATION WITH EMAIL VERIFICATION
        // =============================================

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View(new PatientRegistrationModel());
        }

        // POST: /Account/Register (Step 1: Send verification code)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(PatientRegistrationModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.FullName = CapitalizeName(model.FullName);

                    // Check if email already exists
                    if (IsEmailExists(model.Email))
                    {
                        ModelState.AddModelError("Email", "Email already registered");
                        return View(model);
                    }

                    // Generate verification code
                    string verificationCode = GenerateVerificationCode();

                    // Save all data to session as JSON string
                    var registrationData = new Dictionary<string, string>
                    {
                        ["Email"] = model.Email,
                        ["FullName"] = model.FullName,
                        ["DateOfBirth"] = model.DateOfBirth.ToString("yyyy-MM-dd"),
                        ["Gender"] = model.Gender,
                        ["PhoneNumber"] = model.PhoneNumber,
                        ["Address"] = model.Address ?? "",
                        ["BloodGroup"] = model.BloodGroup ?? "",
                        ["EmergencyContact"] = model.EmergencyContact ?? "",
                        ["Password"] = model.Password
                    };

                    // Save as JSON string
                    string jsonData = System.Text.Json.JsonSerializer.Serialize(registrationData);

                    // Save to multiple session keys for redundancy
                    HttpContext.Session.SetString($"RegistrationData_{model.Email}", jsonData);
                    HttpContext.Session.SetString($"RegistrationData", jsonData);

                    // Also save verification code
                    HttpContext.Session.SetString($"RegCode_{model.Email}", verificationCode);
                    HttpContext.Session.SetString($"RegEmail", model.Email);

                    // Send verification email
                    SendVerificationEmail(model.Email, verificationCode, model.FullName);

                    // ✅ শুধু TempData সেট করুন, Keep() দরকার নেই
                    TempData["SuccessMessage"] = "Verification code sent to your email";

                    return RedirectToAction("VerifyEmail", new { email = model.Email });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Registration failed. Please try again.");
                    Console.WriteLine($"Registration error: {ex.Message}");
                }
            }

            return View(model);
        }

        // GET: /Account/VerifyEmail
        public IActionResult VerifyEmail(string email)
        {
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }

            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            }

            // IMPORTANT: Clear ALL appointment-related data
            HttpContext.Session.Remove("AppointmentLoginRequired");

            // Use email from parameter if provided, otherwise from session
            if (string.IsNullOrEmpty(email))
            {
                email = HttpContext.Session.GetString($"RegEmail");
            }

            if (string.IsNullOrEmpty(email))
            {
                ViewBag.ErrorMessage = "Please start registration process first";
                // TempData তে না সেট করে সরাসরি ViewBag এ সেট করুন
                return RedirectToAction("Register");
            }

            // Get registration data from session
            string jsonData = HttpContext.Session.GetString($"RegistrationData_{email}");

            // If jsonData not found with email, try to find with RegEmail
            if (string.IsNullOrEmpty(jsonData))
            {
                var regEmail = HttpContext.Session.GetString($"RegEmail");
                if (!string.IsNullOrEmpty(regEmail))
                {
                    email = regEmail;
                    jsonData = HttpContext.Session.GetString($"RegistrationData_{email}");
                }
            }

            if (string.IsNullOrEmpty(jsonData))
            {
                ViewBag.ErrorMessage = "Registration data not found. Please start again.";
                return RedirectToAction("Register");
            }

            try
            {
                // Deserialize registration data
                var registrationData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);

                // Create model with default values if keys don't exist
                var model = new RegisterWithVerificationModel
                {
                    Email = registrationData.ContainsKey("Email") ? registrationData["Email"] : email,
                    FullName = registrationData.ContainsKey("FullName") ? registrationData["FullName"] : "",
                    DateOfBirth = registrationData.ContainsKey("DateOfBirth") ?
                                  DateTime.Parse(registrationData["DateOfBirth"]) : DateTime.Now,
                    Gender = registrationData.ContainsKey("Gender") ? registrationData["Gender"] : "",
                    PhoneNumber = registrationData.ContainsKey("PhoneNumber") ? registrationData["PhoneNumber"] : "",
                    Address = registrationData.ContainsKey("Address") ? registrationData["Address"] : "",
                    BloodGroup = registrationData.ContainsKey("BloodGroup") ? registrationData["BloodGroup"] : "",
                    EmergencyContact = registrationData.ContainsKey("EmergencyContact") ? registrationData["EmergencyContact"] : "",
                    Password = registrationData.ContainsKey("Password") ? registrationData["Password"] : "",
                    ConfirmPassword = registrationData.ContainsKey("Password") ? registrationData["Password"] : "",
                    VerificationCode = ""
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Invalid registration data. Please start again.";
                Console.WriteLine($"Error loading registration data: {ex.Message}");
                return RedirectToAction("Register");
            }
        }

        // POST: /Account/ResendRegisterCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResendRegisterCode(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Email is required";
                return RedirectToAction("Register");
            }

            try
            {
                // Check if we have registration data for this email
                string jsonData = HttpContext.Session.GetString($"RegistrationData_{email}");

                if (string.IsNullOrEmpty(jsonData))
                {
                    // Try to get from RegEmail session
                    var sessionEmail = HttpContext.Session.GetString($"RegEmail");
                    if (!string.IsNullOrEmpty(sessionEmail) && sessionEmail != email)
                    {
                        // If session email is different, use it
                        email = sessionEmail;
                        jsonData = HttpContext.Session.GetString($"RegistrationData_{email}");
                    }
                }

                if (string.IsNullOrEmpty(jsonData))
                {
                    TempData["ErrorMessage"] = "Registration session expired. Please start again.";
                    return RedirectToAction("Register");
                }

                // Deserialize to get full name
                var registrationData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);
                string fullName = registrationData["FullName"];

                // Generate new verification code
                string verificationCode = GenerateVerificationCode();

                // Save new verification code to session
                HttpContext.Session.SetString($"RegCode_{email}", verificationCode);

                // Update RegEmail session
                HttpContext.Session.SetString($"RegEmail", email);

                // Send verification email
                SendVerificationEmail(email, verificationCode, fullName);

                // ✅ শুধু TempData সেট করুন
                TempData["SuccessMessage"] = "New verification code has been sent to your email";

                // Return to VerifyEmail view
                return RedirectToAction("VerifyEmail", new { email = email });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to resend code. Please try again.";
                Console.WriteLine($"Resend code error: {ex.Message}");
                return RedirectToAction("Register");
            }
        }

        // POST: /Account/VerifyEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyEmail(RegisterWithVerificationModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Verify code
                    var savedCode = HttpContext.Session.GetString($"RegCode_{model.Email}");
                    if (string.IsNullOrEmpty(savedCode) || savedCode != model.VerificationCode)
                    {
                        ModelState.AddModelError("VerificationCode", "Invalid verification code");
                        return View(model);
                    }

                    // Check if email already exists (double check)
                    if (IsEmailExists(model.Email))
                    {
                        TempData["ErrorMessage"] = "Email already registered";
                        return RedirectToAction("Register");
                    }

                    // Hash password
                    string passwordHash = HashPassword(model.Password);

                    // Register patient using stored procedure
                    int patientId = 0;
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        var cmd = new SqlCommand("sp_RegisterPatient", connection);
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        cmd.Parameters.AddWithValue("@Email", model.Email);
                        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        cmd.Parameters.AddWithValue("@FullName", model.FullName);
                        cmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth);
                        cmd.Parameters.AddWithValue("@Gender", model.Gender);
                        cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
                        cmd.Parameters.AddWithValue("@Address",
                            string.IsNullOrEmpty(model.Address) ? (object)DBNull.Value : model.Address);
                        cmd.Parameters.AddWithValue("@BloodGroup",
                            string.IsNullOrEmpty(model.BloodGroup) ? (object)DBNull.Value : model.BloodGroup);
                        cmd.Parameters.AddWithValue("@EmergencyContact",
                            string.IsNullOrEmpty(model.EmergencyContact) ? (object)DBNull.Value : model.EmergencyContact);

                        // Output parameter
                        var patientIdParam = new SqlParameter("@PatientId", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(patientIdParam);

                        cmd.ExecuteNonQuery();

                        patientId = (int)patientIdParam.Value;
                    }

                    // Generate Display ID
                    string displayId = GenerateDisplayId(patientId, "Patient");

                    // Clear session data
                    ClearRegistrationSession(model.Email);

                    // Auto login after registration
                    AutoLoginPatient(patientId, model.Email, model.FullName, displayId);

                    TempData["RegistrationSuccess"] = true;
                    TempData["PatientId"] = displayId;

                    return RedirectToAction("Dashboard", "Patient");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Registration failed: {ex.Message}");
                    Console.WriteLine($"Registration error: {ex.Message}");
                }
            }

            return View(model);
        }

        // =============================================
        // LOGIN FOR ALL USERS (FIXED)
        // =============================================

        // GET: /Account/Login
        public IActionResult Login(string returnUrl = null)
        {
            // Check if coming from appointment booking attempt
            var appointmentRequired = HttpContext.Session.GetString("AppointmentLoginRequired");

            // Clear the session immediately after checking
            HttpContext.Session.Remove("AppointmentLoginRequired");

            // Clear ALL TempData on Login page load
            TempData.Clear();

            // If coming from appointment, set ViewBag message
            if (!string.IsNullOrEmpty(appointmentRequired) && appointmentRequired == "true")
            {
                ViewBag.AppointmentWarning = "Please login first to book an appointment";
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login (CORRECTED VERSION)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        // Extract UserId from Display ID if it's a Patient/Doctor/Admin/Nurse ID
                        int? extractedUserId = null;
                        string input = model.EmailOrUsername.Trim();

                        // Check if it's a Display ID format
                        if (input.Length > 1 && char.IsLetter(input[0]) &&
                            (input.StartsWith("P") || input.StartsWith("A") ||
                             input.StartsWith("D") || input.StartsWith("N")))
                        {
                            string numbers = input.Substring(1);

                            if (input.StartsWith("P") && int.TryParse(numbers, out int patientDisplayId))
                            {
                                // P100001 format থেকে UserId বের করবো
                                extractedUserId = patientDisplayId - 99000;
                                if (extractedUserId <= 0) extractedUserId = null;
                            }
                            else if (input.StartsWith("A") && int.TryParse(numbers, out int adminDisplayId))
                            {
                                extractedUserId = adminDisplayId;
                            }
                            else if (input.StartsWith("D") && int.TryParse(numbers, out int doctorDisplayId))
                            {
                                extractedUserId = doctorDisplayId - 9000;
                                if (extractedUserId <= 0) extractedUserId = null;
                            }
                            else if (input.StartsWith("N") && int.TryParse(numbers, out int nurseDisplayId))
                            {
                                extractedUserId = nurseDisplayId - 9000;
                                if (extractedUserId <= 0) extractedUserId = null;
                            }
                        }

                        // FIXED QUERY - Use VIEW for Display ID calculation
                        string query = @"
                    SELECT u.UserId, u.Email, u.PasswordHash, u.FullName, u.UserType, u.Username,
                           u.ProfilePicture, -- ✅ প্রোফাইল পিকচার যোগ করুন
                           CASE 
                               WHEN u.UserType = 'Patient' THEN 'P' + CAST((u.UserId + 99000) AS VARCHAR(10))
                               WHEN u.UserType = 'Doctor' THEN 'D' + CAST((u.UserId + 9000) AS VARCHAR(10))
                               WHEN u.UserType = 'Nurse' THEN 'N' + CAST((u.UserId + 9000) AS VARCHAR(10))
                               WHEN u.UserType = 'Admin' THEN 'A' + CAST(u.UserId AS VARCHAR(10))
                               ELSE CAST(u.UserId AS VARCHAR(20))
                           END as DisplayId
                    FROM Users u
                    WHERE (u.Email = @EmailOrUsername 
                           OR u.Username = @EmailOrUsername)";

                        // Add condition for Display ID if extractedUserId is found
                        if (extractedUserId.HasValue)
                        {
                            query += " OR u.UserId = @ExtractedUserId";
                        }

                        query += " AND u.IsActive = 1";

                        var cmd = new SqlCommand(query, connection);
                        cmd.Parameters.AddWithValue("@EmailOrUsername", model.EmailOrUsername);

                        if (extractedUserId.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@ExtractedUserId", extractedUserId.Value);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var userId = reader.GetInt32(0);
                                var email = reader.GetString(1);
                                var storedHash = reader["PasswordHash"].ToString();
                                var fullName = reader.GetString(3);
                                var userType = reader.GetString(4);
                                var displayId = reader["DisplayId"].ToString();

                                // ✅ প্রোফাইল পিকচার পড়ুন
                                var profilePicture = reader["ProfilePicture"]?.ToString() ?? "default.jpg";

                                var inputHash = HashPassword(model.Password);

                                if (storedHash == inputHash)
                                {
                                    // Store in session
                                    HttpContext.Session.SetString("UserId", userId.ToString());
                                    HttpContext.Session.SetString("DisplayId", displayId);
                                    HttpContext.Session.SetString("UserEmail", email);
                                    HttpContext.Session.SetString("UserName", fullName);
                                    HttpContext.Session.SetString("UserType", userType);
                                    HttpContext.Session.SetString("ProfilePicture", profilePicture); // ✅ প্রোফাইল পিকচার সেশন সেট করুন

                                    // ✅ Remember Me এর ভিত্তিতে Session Timeout সেট করুন
                                    if (model.RememberMe)
                                    {
                                        // Remember Me checked হলে 30 দিন
                                        HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString());
                                        HttpContext.Session.SetString("RememberMe", "true");
                                        HttpContext.Session.SetString("LastActivity", DateTime.Now.ToString());

                                        // Session 30 দিনের জন্য
                                        HttpContext.Session.SetInt32("RememberMeDays", 30);
                                    }
                                    else
                                    {
                                        // Remember Me না checked হলে 24 ঘণ্টা
                                        HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString());
                                        HttpContext.Session.Remove("RememberMe");
                                        HttpContext.Session.SetString("LastActivity", DateTime.Now.ToString());

                                        // Session 24 ঘণ্টার জন্য
                                        HttpContext.Session.SetInt32("RememberMeDays", 1);
                                    }

                                    // Create authentication cookie with PROPER settings
                                    var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                                new Claim(ClaimTypes.Email, email),
                                new Claim(ClaimTypes.Name, fullName),
                                new Claim(ClaimTypes.Role, userType),
                                new Claim("DisplayId", displayId),
                                new Claim("RememberMe", model.RememberMe ? "true" : "false"),
                                new Claim("ProfilePicture", profilePicture)
                            };

                                    var identity = new ClaimsIdentity(claims,
                                        CookieAuthenticationDefaults.AuthenticationScheme);
                                    var principal = new ClaimsPrincipal(identity);

                                    // ✅ Remember Me এর ভিত্তিতে Cookie Expiry সেট করুন
                                    var authProperties = new AuthenticationProperties
                                    {
                                        IsPersistent = model.RememberMe, // ✅ Remember Me checkbox এর মান
                                        ExpiresUtc = model.RememberMe ?
                                            DateTimeOffset.UtcNow.AddDays(30) : // ✅ 30 দিন Remember Me হলে
                                            DateTimeOffset.UtcNow.AddHours(24),  // ✅ 24 ঘণ্টা (default)
                                        AllowRefresh = true,
                                        IssuedUtc = DateTimeOffset.UtcNow
                                    };

                                    // ✅ Cookie options সেট করুন
                                    authProperties.Parameters.Add("CookieOptions", new CookieOptions
                                    {
                                        HttpOnly = true,
                                        Secure = true,
                                        SameSite = SameSiteMode.Lax,
                                        MaxAge = model.RememberMe ?
                                            TimeSpan.FromDays(30) :
                                            TimeSpan.FromHours(24),
                                        IsEssential = true
                                    });

                                    await HttpContext.SignInAsync(
                                        CookieAuthenticationDefaults.AuthenticationScheme,
                                        principal,
                                        authProperties);

                                    // ✅ লগইন সফল হলে ডাটাবেসে লগ আপডেট করুন
                                    UpdateLastLogin(userId);

                                    // Redirect based on user type
                                    return RedirectToDashboard(userType);
                                }
                                else
                                {
                                    // Specific error messages
                                    if (input.StartsWith("P") || input.StartsWith("A") ||
                                        input.StartsWith("D") || input.StartsWith("N"))
                                    {
                                        ModelState.AddModelError("Password", $"Incorrect password for {model.EmailOrUsername}");
                                    }
                                    else
                                    {
                                        ModelState.AddModelError("Password", "Incorrect password");
                                    }
                                }
                            }
                            else
                            {
                                // User not found
                                if (input.Contains("@"))
                                {
                                    ModelState.AddModelError("EmailOrUsername", "Email address not found");
                                }
                                else if (input.StartsWith("P") || input.StartsWith("A") ||
                                         input.StartsWith("D") || input.StartsWith("N"))
                                {
                                    ModelState.AddModelError("EmailOrUsername", $"No account found with ID: {model.EmailOrUsername}");
                                }
                                else
                                {
                                    ModelState.AddModelError("EmailOrUsername", "Username not found");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Login failed: {ex.Message}");
                    Console.WriteLine($"Login error: {ex.Message}");
                }
            }

            return View(model);
        }

        // =============================================
        // FORGOT PASSWORD
        // =============================================

        // GET: /Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email exists
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        var cmd = new SqlCommand(
                            "SELECT COUNT(*) FROM Users WHERE Email = @Email AND IsActive = 1",
                            connection);
                        cmd.Parameters.AddWithValue("@Email", model.Email);

                        var exists = (int)cmd.ExecuteScalar() > 0;

                        if (!exists)
                        {
                            ModelState.AddModelError("Email", "Email not found in our system.");
                            TempData["ErrorMessage"] = "Email not found in our system. Please check your email address.";
                            return View(model);
                        }

                        // Email exists, generate reset code
                        string resetCode = GenerateResetCode();

                        // Save reset code to database
                        SaveResetCode(model.Email, resetCode);

                        // Send email
                        SendResetEmail(model.Email, resetCode);

                        // Clear any previous session data
                        HttpContext.Session.Remove($"VerifiedCode_{model.Email}");
                        HttpContext.Session.Remove($"VerifiedTime_{model.Email}");

                        TempData["SuccessMessage"] = $"Password reset code has been sent to {model.Email}";
                        TempData["EmailSent"] = model.Email;

                        // Redirect to ResetPassword with email parameter
                        return RedirectToAction("ResetPassword", new { email = model.Email });
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to process your request. Please try again.");
                    TempData["ErrorMessage"] = "Failed to process your request. Please try again.";
                    Console.WriteLine($"Forgot password error: {ex.Message}");
                }
            }

            return View(model);
        }

        // =============================================
        // VERIFY CODE & RESET PASSWORD
        // =============================================

        // POST: /Account/VerifyCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyCode(string Email, string ResetCode)
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(ResetCode))
            {
                TempData["ErrorMessage"] = "Code required";
                return RedirectToAction("ResetPassword", new { email = Email });
            }

            // Validate reset code
            bool isValid = ValidateResetCode(Email, ResetCode);

            if (isValid)
            {
                // Store verified code in session
                HttpContext.Session.SetString($"VerifiedCode_{Email}", ResetCode);

                // ✅ TempData সেট করুন
                TempData["SuccessMessage"] = "Code verified successfully!";

                // Redirect
                return RedirectToAction("ResetPassword", new
                {
                    email = Email,
                    verified = true
                });
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid verification code";
                return RedirectToAction("ResetPassword", new { email = Email });
            }
        }

        // GET: /Account/ResetPassword
        public IActionResult ResetPassword(string email, bool? verified)
        {
            var model = new ResetPasswordViewModel
            {
                Email = email ?? ""
            };

            bool codeVerified = false;

            // Check if code is already verified in session
            if (!string.IsNullOrEmpty(email))
            {
                var verifiedCode = HttpContext.Session.GetString($"VerifiedCode_{email}");
                codeVerified = !string.IsNullOrEmpty(verifiedCode);

                if (codeVerified)
                {
                    model.ResetCode = verifiedCode;
                }
            }

            // If verified parameter is passed, set code verified
            if (verified.HasValue && verified.Value)
            {
                codeVerified = true;
                if (!string.IsNullOrEmpty(email))
                {
                    var verifiedCode = HttpContext.Session.GetString($"VerifiedCode_{email}");
                    if (!string.IsNullOrEmpty(verifiedCode))
                    {
                        model.ResetCode = verifiedCode;
                    }
                }
            }

            ViewBag.CodeVerified = codeVerified;
            ViewBag.Email = email;

            return View(model);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if code was verified in session
                    var verifiedCode = HttpContext.Session.GetString($"VerifiedCode_{model.Email}");

                    if (string.IsNullOrEmpty(verifiedCode) || verifiedCode != model.ResetCode)
                    {
                        TempData["ErrorMessage"] = "Please verify your code first";
                        return RedirectToAction("ResetPassword", new { email = model.Email });
                    }

                    // Validate reset code from database
                    if (!ValidateResetCode(model.Email, model.ResetCode))
                    {
                        TempData["ErrorMessage"] = "Invalid or expired verification code";
                        return RedirectToAction("ResetPassword", new { email = model.Email });
                    }

                    // Update password
                    UpdatePassword(model.Email, model.NewPassword);

                    // Mark reset code as used
                    MarkResetCodeAsUsed(model.Email, model.ResetCode);

                    // Clear session data
                    HttpContext.Session.Remove($"VerifiedCode_{model.Email}");
                    HttpContext.Session.Remove($"VerifiedTime_{model.Email}");

                    ViewBag.ShowSuccessOnPage = true;
                    ViewBag.SuccessMessage = "Password reset successfully! You can now login with your new password.";
                    ViewBag.Email = model.Email;
                    ViewBag.CountdownSeconds = 5;

                    return View(model);
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Failed to reset password: {ex.Message}";
                    ViewBag.CodeVerified = !string.IsNullOrEmpty(HttpContext.Session.GetString($"VerifiedCode_{model.Email}"));
                    return View(model);
                }
            }

            ViewBag.CodeVerified = !string.IsNullOrEmpty(HttpContext.Session.GetString($"VerifiedCode_{model.Email}"));
            return View(model);
        }

        // POST: /Account/ResendCodeOnly
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResendCodeOnly(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Email is required";
                return RedirectToAction("ForgotPassword");
            }

            try
            {
                // Check if email exists
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM Users WHERE Email = @Email AND IsActive = 1",
                        connection);
                    cmd.Parameters.AddWithValue("@Email", email);

                    var exists = (int)cmd.ExecuteScalar() > 0;

                    if (!exists)
                    {
                        TempData["ErrorMessage"] = "Email not found in our system.";
                        return RedirectToAction("ForgotPassword");
                    }

                    // Generate new reset code
                    string resetCode = GenerateResetCode();

                    // Save reset code to database
                    SaveResetCode(email, resetCode);

                    // Send email
                    SendResetEmail(email, resetCode);

                    // IMPORTANT: Clear any previous verified code from session
                    HttpContext.Session.Remove($"VerifiedCode_{email}");
                    HttpContext.Session.Remove($"VerifiedTime_{email}");

                    TempData["SuccessMessage"] = $"New verification code has been sent to {email}";
                    TempData["ResentCode"] = true;

                    return RedirectToAction("ResetPassword", new { email = email });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to resend code. Please try again.";
                Console.WriteLine($"Resend code error: {ex.Message}");
                return RedirectToAction("ForgotPassword");
            }
        }

        // POST: /Account/ResendResetCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResendResetCode(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Email is required";
                return RedirectToAction("ForgotPassword");
            }

            try
            {
                // Check if email exists
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM Users WHERE Email = @Email AND IsActive = 1",
                        connection);
                    cmd.Parameters.AddWithValue("@Email", email);

                    var exists = (int)cmd.ExecuteScalar() > 0;

                    if (!exists)
                    {
                        TempData["ErrorMessage"] = "Email not found in our system.";
                        return RedirectToAction("ForgotPassword");
                    }

                    // Generate new reset code
                    string resetCode = GenerateResetCode();

                    // Save reset code to database
                    SaveResetCode(email, resetCode);

                    // Send email
                    SendResetEmail(email, resetCode);

                    TempData["SuccessMessage"] = "New reset code has been sent to your email.";

                    // Clear any existing verified code
                    HttpContext.Session.Remove($"VerifiedCode_{email}");

                    // Return to ResetPassword page with same email
                    return RedirectToAction("ResetPassword", new { email = email });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to resend code. Please try again.";
                Console.WriteLine($"Resend reset code error: {ex.Message}");
                return RedirectToAction("ForgotPassword");
            }
        }

        // =============================================
        // HELPER METHODS
        // =============================================

        // Remember Me ডাটাবেসে সেভ করার method
        private void UpdateRememberMe(int userId, bool rememberMe)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "UPDATE Users SET RememberMe = @RememberMe, RememberMeExpiry = @Expiry WHERE UserId = @UserId",
                        connection);

                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@RememberMe", rememberMe);

                    if (rememberMe)
                    {
                        cmd.Parameters.AddWithValue("@Expiry", DateTime.Now.AddDays(30));
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@Expiry", DateTime.Now.AddHours(24));
                    }

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update RememberMe error: {ex.Message}");
            }
        }

        // Session expiry চেক করার method
        private void CheckAndExtendSession()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var rememberMe = HttpContext.Session.GetString("RememberMe");

            if (!string.IsNullOrEmpty(userId) && rememberMe == "true")
            {
                // Remember Me enabled হলে session renew করুন
                var loginTimeStr = HttpContext.Session.GetString("LoginTime");

                if (DateTime.TryParse(loginTimeStr, out DateTime loginTime))
                {
                    // 30 দিন পর expire হবে
                    var expiryTime = loginTime.AddDays(30);

                    if (DateTime.Now < expiryTime)
                    {
                        // Session renew করুন
                        HttpContext.Session.SetString("LastActivity", DateTime.Now.ToString());

                        // Cookie renew করার জন্য
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                            AllowRefresh = true
                        };

                        // Current user পুনরুদ্ধার করুন
                        var user = HttpContext.User;
                        if (user.Identity.IsAuthenticated)
                        {
                            HttpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                user,
                                authProperties).Wait();
                        }
                    }
                }
            }
        }

        private void UpdateLastLogin(int userId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "UPDATE Users SET LastLogin = GETDATE() WHERE UserId = @UserId",
                        connection);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update last login error: {ex.Message}");
            }
        }

        private void UpdateActivity(int userId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "UPDATE Users SET LastActivity = GETDATE() WHERE UserId = @UserId",
                        connection);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update activity error: {ex.Message}");
            }
        }

        private bool IsEmailExists(string email)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email", connection);
                cmd.Parameters.AddWithValue("@Email", email);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private string GenerateDisplayId(int userId, string userType)
        {
            return userType switch
            {
                "Patient" => "P" + (userId + 99000).ToString("D6"),
                "Doctor" => "D" + (userId + 9000).ToString("D5"),
                "Nurse" => "N" + (userId + 9000).ToString("D5"),
                "Admin" => "A" + userId.ToString("D4"), // FIXED: A + userId
                _ => userId.ToString()
            };
        }

        private void AutoLoginPatient(int userId, string email, string fullName, string displayId)
        {
            HttpContext.Session.SetString("UserId", userId.ToString());
            HttpContext.Session.SetString("DisplayId", displayId);
            HttpContext.Session.SetString("UserEmail", email);
            HttpContext.Session.SetString("UserName", fullName);
            HttpContext.Session.SetString("UserType", "Patient");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, "Patient")
            };

            var identity = new ClaimsIdentity(claims,
                CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                principal, new AuthenticationProperties
                {
                    ExpiresUtc = DateTime.UtcNow.AddDays(7)
                }).Wait();
        }

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

        // Helper method for name capitalization
        private string CapitalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;

            name = name.Trim();

            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) +
                              (words[i].Length > 1 ? words[i].Substring(1).ToLower() : "");
                }
            }

            return string.Join(" ", words);
        }

        // Helper method
        private string GetRegistrationDataFromSession(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                email = HttpContext.Session.GetString($"RegEmail");
            }

            string jsonData = null;
            if (!string.IsNullOrEmpty(email))
            {
                jsonData = HttpContext.Session.GetString($"RegistrationData_{email}");
            }

            // Fallback
            if (string.IsNullOrEmpty(jsonData))
            {
                jsonData = HttpContext.Session.GetString($"RegistrationData");
            }

            return jsonData;
        }

        // GET: /Account/Logout
        public async Task<IActionResult> Logout()
        {
            try
            {
                // ✅ 1. Clear all session data
                HttpContext.Session.Clear();

                // ✅ 2. Sign out from authentication (CORRECT WAY for .NET 8.0)
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                // ✅ 3. Clear authentication cookie manually
                foreach (var cookie in Request.Cookies.Keys)
                {
                    if (cookie.Contains("Auth") || cookie.Contains("Session") || cookie.Contains(".AspNetCore."))
                    {
                        Response.Cookies.Delete(cookie);
                    }
                }

                // ✅ 4. Add a security header
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";

                TempData["SuccessMessage"] = "You have been logged out successfully.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Logout error: {ex.Message}");

                // Still clear session and redirect
                HttpContext.Session.Clear();
                return RedirectToAction("Index", "Home");
            }
        }

        // =============================================
        // PASSWORD RESET HELPER METHODS
        // =============================================

        private string GenerateResetCode()
        {
            // Generate 6-digit random code
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private void SaveResetCode(string email, string resetCode)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Delete old reset codes for this email
                var deleteCmd = new SqlCommand(
                    "DELETE FROM PasswordResets WHERE Email = @Email",
                    connection);
                deleteCmd.Parameters.AddWithValue("@Email", email);
                deleteCmd.ExecuteNonQuery();

                // Insert new reset code
                var insertCmd = new SqlCommand(@"
                    INSERT INTO PasswordResets (Email, ResetCode, CreatedAt, ExpiresAt, IsUsed)
                    VALUES (@Email, @ResetCode, GETDATE(), DATEADD(MINUTE, 30, GETDATE()), 0)",
                    connection);

                insertCmd.Parameters.AddWithValue("@Email", email);
                insertCmd.Parameters.AddWithValue("@ResetCode", resetCode);
                insertCmd.ExecuteNonQuery();
            }
        }

        private bool ValidateResetCode(string email, string resetCode)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM PasswordResets 
                    WHERE Email = @Email 
                    AND ResetCode = @ResetCode 
                    AND IsUsed = 0 
                    AND ExpiresAt > GETDATE()",
                    connection);

                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@ResetCode", resetCode);

                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        private void MarkResetCodeAsUsed(string email, string resetCode)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(
                    "UPDATE PasswordResets SET IsUsed = 1 WHERE Email = @Email AND ResetCode = @ResetCode",
                    connection);

                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@ResetCode", resetCode);
                cmd.ExecuteNonQuery();
            }
        }

        private void UpdatePassword(string email, string newPassword)
        {
            string passwordHash = HashPassword(newPassword);

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(
                    "UPDATE Users SET PasswordHash = @PasswordHash, UpdatedAt = GETDATE() WHERE Email = @Email",
                    connection);

                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.ExecuteNonQuery();
            }
        }


        // GET: /Admin/DownloadReport
        public IActionResult DownloadReport(string fileName, int reportId)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reports", fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["ErrorMessage"] = "File not found";
                    return RedirectToAction("ViewTestReport", new { reportId = reportId });
                }

                // Get file info for display name
                string displayName = "";
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand("SELECT ReportName FROM TestReports WHERE ReportId = @ReportId", connection);
                    cmd.Parameters.AddWithValue("@ReportId", reportId);
                    displayName = cmd.ExecuteScalar()?.ToString() ?? "TestReport";
                }

                // Clean filename for download
                string downloadName = displayName.Replace(" ", "_") + Path.GetExtension(fileName);

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/octet-stream", downloadName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Download error: {ex.Message}";
                return RedirectToAction("ViewTestReport", new { reportId = reportId });
            }
        }

        // AccountController.cs-এর মধ্যে কোথাও এই method টি যোগ করুন:

        private bool IsAdminLoggedIn()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");
            return !string.IsNullOrEmpty(userId) && userType == "Admin";
        }

        // GET: /Admin/DownloadPrescription
        public IActionResult DownloadPrescription(string fileName, int prescriptionId)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "prescriptions", fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["ErrorMessage"] = "File not found";
                    return RedirectToAction("ViewPrescription", new { prescriptionId = prescriptionId });
                }

                // Get file info for display name
                string displayName = "";
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand("SELECT PrescriptionFile FROM Prescriptions WHERE PrescriptionId = @PrescriptionId", connection);
                    cmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);
                    var originalName = cmd.ExecuteScalar()?.ToString() ?? "Prescription";
                    displayName = Path.GetFileNameWithoutExtension(originalName);
                }

                // Clean filename for download
                string downloadName = displayName.Replace(" ", "_") + Path.GetExtension(fileName);

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/octet-stream", downloadName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Download error: {ex.Message}";
                return RedirectToAction("ViewPrescription", new { prescriptionId = prescriptionId });
            }
        }
        private void SendResetEmail(string email, string resetCode)
        {
            try
            {
                // Get user's full name from database
                string fullName = GetUserNameByEmail(email);

                // Get email settings from configuration
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];

                var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = "Sunrise Clinic - Password Reset Code",
                    Body = GenerateEmailBody(resetCode, fullName),
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(email);
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                // For development/testing, store code in session
                HttpContext.Session.SetString($"ResetCode_{email}", resetCode);
                Console.WriteLine($"Reset code for {email}: {resetCode} (Email sending failed: {ex.Message})");
            }
        }

        private string GenerateEmailBody(string resetCode, string fullName)
        {
            return $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='UTF-8'>
        <style>
            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
            .header {{ background: linear-gradient(135deg, #1a73e8 0%, #00bfae 100%); padding: 30px; text-align: center; }}
            .content {{ padding: 30px; background-color: #f8f9fa; }}
            .code-box {{ 
                background: white; 
                padding: 25px 40px; 
                border-radius: 10px; 
                border: 2px dashed #1a73e8; 
                font-size: 32px; 
                font-weight: bold; 
                letter-spacing: 8px; 
                color: #1a73e8; 
                text-align: center;
                margin: 30px 0;
                display: inline-block;
            }}
            .footer {{ background-color: #f1f1f1; padding: 20px; text-align: center; color: #666; font-size: 12px; }}
            .greeting {{ font-size: 18px; color: #1a73e8; margin-bottom: 20px; }}
        </style>
    </head>
    <body>
        <div class='container'>
            <div class='header'>
                <h2 style='color: white; margin: 0;'>Sunrise Clinic & Diagnostic Center</h2>
            </div>
            <div class='content'>
                <div class='greeting'>
                    <strong>Dear {fullName},</strong>
                </div>
                
                <h3 style='color: #333; margin-top: 0;'>Password Reset Request</h3>
                <p>You have requested to reset your password. Please use the following verification code:</p>
                
                <div style='text-align: center;'>
                    <div class='code-box'>{resetCode}</div>
                </div>
                
                <p>This code will expire in <strong>30 minutes</strong>.</p>
                <p>If you didn't request a password reset, please ignore this email.</p>
                
                <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'>
                
                <p style='color: #666; font-size: 12px;'>
                    <strong>Security Tip:</strong> Never share this code with anyone.<br>
                    Sunrise Clinic team will never ask for your password or verification code.
                </p>
            </div>
            <div class='footer'>
                <p>© {DateTime.Now.Year} Sunrise Clinic & Diagnostic Center. All rights reserved.</p>
                <p>TB Clinic Gate, Shalgaria, Pabna Sadar, Pabna, Bangladesh</p>
            </div>
        </div>
    </body>
    </html>";
        }

        // =============================================
        // REGISTRATION HELPER METHODS
        // =============================================

        private string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        // Helper method to send verification email
        private void SendVerificationEmail(string email, string code, string fullName)
        {
            try
            {
                // Get email settings from configuration
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];

                var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = "Sunrise Clinic - Email Verification Code",
                    Body = GenerateVerificationEmailBody(code, fullName),
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(email);
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                // For development/testing, store code in session
                HttpContext.Session.SetString($"TestCode_{email}", code);
                Console.WriteLine($"Verification code for {email}: {code} (Email sending failed: {ex.Message})");
            }
        }

        private string GetUserNameByEmail(string email)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "SELECT FullName FROM Users WHERE Email = @Email",
                        connection);
                    cmd.Parameters.AddWithValue("@Email", email);

                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "Valued Patient";
                }
            }
            catch (Exception)
            {
                return "Valued Patient";
            }
        }

        private void ClearRegistrationSession(string email)
        {
            HttpContext.Session.Remove($"RegCode_{email}");
            HttpContext.Session.Remove($"RegEmail");
            HttpContext.Session.Remove($"RegName");
            HttpContext.Session.Remove($"RegDOB");
            HttpContext.Session.Remove($"RegGender");
            HttpContext.Session.Remove($"RegPhone");
            HttpContext.Session.Remove($"RegAddress");
            HttpContext.Session.Remove($"RegBlood");
            HttpContext.Session.Remove($"RegEmergency");
            HttpContext.Session.Remove($"RegPassword");
        }

        private string GenerateVerificationEmailBody(string code, string fullName)
        {
            return $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='UTF-8'>
        <style>
            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
            .header {{ background: linear-gradient(135deg, #1a73e8 0%, #00bfae 100%); padding: 30px; text-align: center; }}
            .content {{ padding: 30px; background-color: #f8f9fa; }}
            .code-box {{ 
                background: white; 
                padding: 25px 40px; 
                border-radius: 10px; 
                border: 2px dashed #1a73e8; 
                font-size: 32px; 
                font-weight: bold; 
                letter-spacing: 8px; 
                color: #1a73e8; 
                text-align: center;
                margin: 30px 0;
                display: inline-block;
            }}
            .footer {{ background-color: #f1f1f1; padding: 20px; text-align: center; color: #666; font-size: 12px; }}
            .greeting {{ font-size: 18px; color: #1a73e8; margin-bottom: 20px; }}
        </style>
    </head>
    <body>
        <div class='container'>
            <div class='header'>
                <h2 style='color: white; margin: 0;'>Sunrise Clinic & Diagnostic Center</h2>
            </div>
            <div class='content'>
                <div class='greeting'>
                    <strong>Hello {fullName},</strong>
                </div>
                
                <h3 style='color: #333; margin-top: 0;'>Email Verification</h3>
                <p>Thank you for registering with Sunrise Clinic. Please use the following verification code to complete your registration:</p>
                
                <div style='text-align: center;'>
                    <div class='code-box'>{code}</div>
                </div>
                
                <p>This code will expire in <strong>30 minutes</strong>.</p>
                <p>If you didn't request to register, please ignore this email.</p>
                
                <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'>
                
                <p style='color: #666; font-size: 12px;'>
                    <strong>Security Tip:</strong> Never share this code with anyone.<br>
                    Sunrise Clinic team will never ask for your verification code.
                </p>
            </div>
            <div class='footer'>
                <p>© {DateTime.Now.Year} Sunrise Clinic & Diagnostic Center. All rights reserved.</p>
                <p>TB Clinic Gate, Shalgaria, Pabna Sadar, Pabna, Bangladesh</p>
            </div>
        </div>
    </body>
    </html>";
        }

        // =============================================
        // DEBUG AND TEST METHODS
        // =============================================

        [AllowAnonymous]
        public IActionResult TestLogin()
        {
            // Admin এর জন্য test hash
            var testPassword = "Admin@123";
            var hash = HashPassword(testPassword);

            ViewBag.TestPassword = testPassword;
            ViewBag.TestHash = hash;

            // Database থেকে hash নেয়া
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(
                    "SELECT PasswordHash FROM Users WHERE Email = 'admin@sunriseclinicbd.com'",
                    connection);
                var dbHash = cmd.ExecuteScalar()?.ToString();

                ViewBag.DbHash = dbHash;
                ViewBag.Match = hash == dbHash;
            }

            return View();
        }

        [AllowAnonymous]
        public IActionResult DirectAdminLogin()
        {
            try
            {
                // Direct Admin login
                var email = "admin@sunriseclinicbd.com";
                var password = "Admin@123";

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new SqlCommand(
                        "SELECT UserId, Email, PasswordHash, FullName, UserType FROM Users WHERE Email = @Email",
                        connection);
                    cmd.Parameters.AddWithValue("@Email", email);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var userId = reader.GetInt32(0);
                            var dbEmail = reader.GetString(1);
                            var dbHash = reader.GetString(2);
                            var fullName = reader.GetString(3);
                            var userType = reader.GetString(4);

                            var inputHash = HashPassword(password);

                            if (dbHash == inputHash)
                            {
                                // Login successful
                                HttpContext.Session.SetString("UserId", userId.ToString());
                                HttpContext.Session.SetString("UserEmail", dbEmail);
                                HttpContext.Session.SetString("UserName", fullName);
                                HttpContext.Session.SetString("UserType", userType);

                                // Generate Display ID
                                string displayId = userType switch
                                {
                                    "Patient" => "P" + (userId + 99000).ToString("D6"),
                                    "Doctor" => "D" + (userId + 9000).ToString("D5"),
                                    "Nurse" => "N" + (userId + 9000).ToString("D5"),
                                    "Admin" => "A" + userId.ToString("D4"),
                                    _ => userId.ToString()
                                };

                                HttpContext.Session.SetString("DisplayId", displayId);

                                TempData["SuccessMessage"] = $"Admin login successful! ID: {displayId}";
                                return RedirectToAction("Dashboard", "Admin");
                            }
                        }
                    }
                }

                TempData["ErrorMessage"] = "Direct login failed";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Login");
            }
        }
    }
}