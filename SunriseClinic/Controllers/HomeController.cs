using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Net.Sockets;

namespace SunriseClinic.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Gallery()
        {
            return View();
        }

        public IActionResult Emergency()
        {
            return View();
        }

        public IActionResult Diagnostic()
        {
            return View();
        }

        public IActionResult OPD()
        {
            return View();
        }

        public IActionResult Checkup()
        {
            return View();
        }

        public IActionResult Ambulance()
        {
            return View();
        }

        public IActionResult Services()
        {
            return View();
        }

        public IActionResult Developer()
        {
            return View();
        }

        public IActionResult SeoCaseStudy()
        {
            ViewData["Title"] = "SEO Case Study (Bing-Focused) - MD Nadim Mostaq Eman";
            ViewData["Description"] = "Bing-focused SEO case study for MakeIn10.com: ranking without backlinks, indexing strategy, and international traffic insights with screenshots.";
            return View();
        }

        [Route("/Home/DatabaseError")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult DatabaseError()
        {
            // Exception theke error message access koro
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;

            // Session theke o error message pete paren
            var sessionError = HttpContext.Session.GetString("DatabaseError");

            string errorMessage = "Database connection failed. Please try again later.";

            if (!string.IsNullOrEmpty(sessionError))
            {
                errorMessage = sessionError;
            }
            else if (exception != null)
            {
                if (exception is SqlException sqlEx)
                {
                    errorMessage = $"SQL Error ({sqlEx.Number}): {sqlEx.Message}";
                }
                else if (exception is SocketException)
                {
                    errorMessage = "Cannot connect to database server. Check network connection.";
                }
                else if (exception.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = exception.Message;
                }
            }

            ViewBag.ErrorMessage = errorMessage;
            return View();
        }
    }
}