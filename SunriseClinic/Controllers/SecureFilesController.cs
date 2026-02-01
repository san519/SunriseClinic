using Microsoft.AspNetCore.Mvc;

public class SecureFilesController : Controller
{
    [HttpGet("/secure/cert/{fileName}")]
    public IActionResult Cert(string fileName)
    {
        // whitelist
        var allowed = new[] { "certificate1.webp", "certificate2.webp" };
        if (!allowed.Contains(fileName)) return NotFound();

        // Keep outside wwwroot:
        var path = Path.Combine(Directory.GetCurrentDirectory(), "PrivateFiles", "Nadim", fileName);
        if (!System.IO.File.Exists(path)) return NotFound();

        // No-store headers (still not a perfect protection)
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers["Pragma"] = "no-cache";

        return PhysicalFile(path, "image/webp"); // inline
    }
}
