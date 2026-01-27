using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Xml;

namespace SunriseClinic.Controllers
{
    [Route("sitemap.xml")]
    public class SitemapController : Controller
    {
        private readonly ILogger<SitemapController> _logger;

        public SitemapController(ILogger<SitemapController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                var sitemapContent = GenerateSitemap();
                return Content(sitemapContent, "application/xml", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sitemap");
                return StatusCode(500, "Error generating sitemap");
            }
        }

        private string GenerateSitemap()
        {
            var baseUrl = "https://www.sunriseclinicbd.com";
            var today = DateTime.Now.ToString("yyyy-MM-dd");

            var stringBuilder = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                OmitXmlDeclaration = false
            };

            using (var writer = XmlWriter.Create(stringBuilder, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

                // Main URLs with priorities
                var pages = new[]
                {
                    new { Url = "/", Priority = "1.0", Frequency = "daily" },
                    new { Url = "/Home/Diagnostic", Priority = "0.9", Frequency = "weekly" },
                    new { Url = "/Home/Contact", Priority = "0.8", Frequency = "monthly" },
                    new { Url = "/Home/About", Priority = "0.8", Frequency = "monthly" },
                    new { Url = "/Home/Emergency", Priority = "0.8", Frequency = "monthly" },
                    new { Url = "/Home/Gallery", Priority = "0.7", Frequency = "monthly" },
                    new { Url = "/Home/OPD", Priority = "0.7", Frequency = "monthly" },
                    new { Url = "/Home/Checkup", Priority = "0.7", Frequency = "monthly" },
                    new { Url = "/Home/Ambulance", Priority = "0.7", Frequency = "monthly" },
                    new { Url = "/Home/Services", Priority = "0.7", Frequency = "monthly" },
                    new { Url = "/Home/Developer", Priority = "0.3", Frequency = "yearly" },
                    new { Url = "/Home/Privacy", Priority = "0.3", Frequency = "yearly" },
                    new { Url = "/Account/Login", Priority = "0.5", Frequency = "monthly" },
                    new { Url = "/Account/Register", Priority = "0.5", Frequency = "monthly" },
                    new { Url = "/Appointment/Create", Priority = "0.8", Frequency = "daily" },
                    new { Url = "/Departments", Priority = "0.7", Frequency = "monthly" },
                    new { Url = "/Doctors", Priority = "0.7", Frequency = "monthly" }
                };

                foreach (var page in pages)
                {
                    writer.WriteStartElement("url");
                    writer.WriteElementString("loc", baseUrl + page.Url);
                    writer.WriteElementString("lastmod", today);
                    writer.WriteElementString("changefreq", page.Frequency);
                    writer.WriteElementString("priority", page.Priority);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return stringBuilder.ToString();
        }
    }
}