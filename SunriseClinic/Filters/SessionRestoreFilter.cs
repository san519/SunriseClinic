using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SunriseClinic.Filters
{
    public class SessionRestoreFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;

            // Skip static files
            var path = httpContext.Request.Path.Value ?? "";
            if (path.Contains(".css") || path.Contains(".js") || path.Contains(".png") ||
                path.Contains(".jpg") || path.Contains(".webp") || path.Contains("/api/"))
                return;

            // শুধু authenticated users এর জন্য
            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                // UserId claim থেকে নিন
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Session এ UserId আছে কিনা চেক করুন
                    var existingUserId = httpContext.Session.GetString("UserId");

                    // যদি Session এ UserId না থাকে, তাহলে restore করুন
                    if (string.IsNullOrEmpty(existingUserId))
                    {
                        // ✅ BASIC Session Data Restore
                        httpContext.Session.SetString("UserId", userId.ToString());

                        var userType = httpContext.User.FindFirst(ClaimTypes.Role)?.Value ?? "";
                        httpContext.Session.SetString("UserType", userType);

                        var userName = httpContext.User.Identity?.Name ?? "";
                        httpContext.Session.SetString("UserName", userName);

                        var userEmail = httpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? "";
                        httpContext.Session.SetString("UserEmail", userEmail);

                        var profilePicture = httpContext.User.FindFirst("ProfilePicture")?.Value ?? "default.webp";
                        httpContext.Session.SetString("ProfilePicture", profilePicture);

                        var displayId = httpContext.User.FindFirst("DisplayId")?.Value ?? "";
                        if (string.IsNullOrEmpty(displayId))
                        {
                            // Generate display ID
                            displayId = userType switch
                            {
                                "Patient" => "P" + (userId + 99000),
                                "Doctor" => "D" + (userId + 9000),
                                "Nurse" => "N" + (userId + 9000),
                                "Admin" => "A" + userId,
                                _ => userId.ToString()
                            };
                        }
                        httpContext.Session.SetString("DisplayId", displayId);

                        // ✅ Remember Me সেট করুন
                        var rememberMe = httpContext.User.FindFirst("RememberMe")?.Value == "true";
                        httpContext.Session.SetString("RememberMe", rememberMe.ToString());

                        // ✅ LastActivity timestamp
                        httpContext.Session.SetString("LastActivity", DateTime.Now.ToString());

                        // ✅ LoginTime (প্রথম বার)
                        if (string.IsNullOrEmpty(httpContext.Session.GetString("LoginTime")))
                        {
                            httpContext.Session.SetString("LoginTime", DateTime.Now.ToString());
                        }

                        Console.WriteLine($"✅ Session restored for {userName} (RememberMe: {rememberMe})");
                    }
                    else
                    {
                        // ✅ Session exists, update LastActivity for auto-logout tracking
                        httpContext.Session.SetString("LastActivity", DateTime.Now.ToString());

                        // ✅ Check if session should expire based on RememberMe
                        CheckSessionExpiry(httpContext);
                    }
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Nothing needed
        }

        // ✅ Check if session should expire
        private void CheckSessionExpiry(HttpContext httpContext)
        {
            var rememberMe = httpContext.Session.GetString("RememberMe") == "true";
            var lastActivityStr = httpContext.Session.GetString("LastActivity");

            if (!string.IsNullOrEmpty(lastActivityStr) && DateTime.TryParse(lastActivityStr, out DateTime lastActivity))
            {
                var timeElapsed = DateTime.Now - lastActivity;

                if (rememberMe)
                {
                    // Remember Me = 30 days
                    if (timeElapsed.TotalDays > 30)
                    {
                        Console.WriteLine($"⚠️ RememberMe session expired (30 days)");
                        // AutoLogoutService handle করবে
                    }
                }
                else
                {
                    // No Remember Me = 24 hours
                    if (timeElapsed.TotalHours > 24)
                    {
                        Console.WriteLine($"⚠️ Regular session expired (24 hours)");
                        // AutoLogoutService handle করবে
                    }
                }
            }
        }
    }
}