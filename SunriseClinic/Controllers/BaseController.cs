using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace SunriseClinic.Controllers
{
    public class BaseController : Controller
    {
        protected readonly IConfiguration _configuration;
        protected readonly string _connectionString;

        public BaseController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        protected bool IsLoggedIn()
        {
            var userId = HttpContext.Session.GetString("UserId");
            return !string.IsNullOrEmpty(userId);
        }

        protected bool IsUserType(string userType)
        {
            var currentUserType = HttpContext.Session.GetString("UserType");
            return currentUserType == userType;
        }

        protected int GetCurrentUserId()
        {
            var userId = HttpContext.Session.GetString("UserId");
            return int.Parse(userId ?? "0");
        }

        protected string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}