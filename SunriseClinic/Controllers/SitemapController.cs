using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Xml;

namespace SunriseClinic.Controllers
{
    [Route("sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public class SitemapController : Controller
    {
        private readonly ILogger<SitemapController> _logger;
        private readonly IConfiguration _configuration;

        public SitemapController(ILogger<SitemapController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
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
            // ✅ তোমার preferred canonical base (একটাই রাখো সবখানে)
            var baseUrl = "https://www.sunriseclinicbd.com";
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var urls = new List<SitemapUrl>();

            // ✅ Home public pages
            urls.AddRange(new[]
            {
                new SitemapUrl("/", "1.0", "daily"),
                new SitemapUrl("/Home/Diagnostic", "0.9", "weekly"),
                new SitemapUrl("/Home/Services", "0.8", "monthly"),
                new SitemapUrl("/Home/About", "0.8", "monthly"),
                new SitemapUrl("/Home/Contact", "0.8", "monthly"),
                new SitemapUrl("/Home/Emergency", "0.8", "monthly"),
                new SitemapUrl("/Home/Gallery", "0.7", "monthly"),
                new SitemapUrl("/Home/OPD", "0.7", "monthly"),
                new SitemapUrl("/Home/Checkup", "0.7", "monthly"),
                new SitemapUrl("/Home/Ambulance", "0.7", "monthly"),

                // ✅ Portfolio + case study (rank target)
                new SitemapUrl("/Home/Developer", "0.9", "monthly"),
                new SitemapUrl("/Home/SeoCaseStudy", "0.9", "monthly"),

                // ✅ low priority
                new SitemapUrl("/Home/Privacy", "0.2", "yearly"),
            });

            // ✅ Departments public pages
            urls.AddRange(new[]
            {
                new SitemapUrl("/Departments", "0.7", "monthly"),
                new SitemapUrl("/Departments/ChestAndTBDiseases", "0.6", "yearly"),
                new SitemapUrl("/Departments/GynecologyObstetrics", "0.6", "yearly"),
                new SitemapUrl("/Departments/LaparoscopicSurgery", "0.6", "yearly"),
                new SitemapUrl("/Departments/DiagnosticCenter", "0.6", "yearly"),
                new SitemapUrl("/Departments/Administration", "0.6", "yearly"),
            });

            // ✅ Doctors list + dynamic details
            urls.Add(new SitemapUrl("/Doctors", "0.7", "weekly"));
            foreach (var doctorId in GetActiveDoctorIds())
            {
                urls.Add(new SitemapUrl($"/Doctors/Details/{doctorId}", "0.6", "weekly"));
            }

            // XML build
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                OmitXmlDeclaration = false
            };

            using (var writer = XmlWriter.Create(sb, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

                foreach (var u in urls.DistinctBy(x => x.Url))
                {
                    writer.WriteStartElement("url");
                    writer.WriteElementString("loc", baseUrl.TrimEnd('/') + u.Url);
                    writer.WriteElementString("lastmod", today);
                    writer.WriteElementString("changefreq", u.ChangeFreq);
                    writer.WriteElementString("priority", u.Priority);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return sb.ToString();
        }

        private IEnumerable<int> GetActiveDoctorIds()
        {
            var ids = new List<int>();

            try
            {
                var cs = _configuration.GetConnectionString("DefaultConnection");
                using var con = new SqlConnection(cs);
                con.Open();

                // ✅ ZIP project অনুযায়ী Users table থেকে active doctor list
                var sql = @"
                    SELECT u.UserId
                    FROM Users u
                    WHERE u.UserType = 'Doctor' AND u.IsActive = 1
                    ORDER BY u.UserId";

                using var cmd = new SqlCommand(sql, con);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    ids.Add(r.GetInt32(0));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load doctor ids for sitemap");
            }

            return ids;
        }

        private record SitemapUrl(string Url, string Priority, string ChangeFreq);
    }

    static class LinqHelpers
    {
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
        {
            var seen = new HashSet<TKey>();
            foreach (var item in source)
            {
                if (seen.Add(keySelector(item)))
                    yield return item;
            }
        }
    }
}
