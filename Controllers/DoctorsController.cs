using Microsoft.AspNetCore.Mvc;
using PEI_Doctors.Services;

namespace PEI_Doctors.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly DoctorMonitorService _doctorService;

    public DoctorsController(DoctorMonitorService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpPost("scrape")]
    public async Task<IActionResult> Scrape()
    {
        try
        {
            var report = await _doctorService.RunScrapeAsync();
            return Ok(new { message = "Scrape completed successfully", report });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Scrape failed", error = ex.Message });
        }
    }

    [HttpGet("baseline")]
    public async Task<IActionResult> GetBaseline()
    {
        string dataDir = Path.Combine(_doctorService.GetProjectRoot(), "data");
        string baselineFile = Path.Combine(dataDir, "baseline.json");

        if (!System.IO.File.Exists(baselineFile))
        {
            return NotFound("Baseline file not found");
        }

        var content = await System.IO.File.ReadAllTextAsync(baselineFile);
        return Content(content, "application/json");
    }

    [HttpGet("dates")]
    public IActionResult GetAvailableDates()
    {
        string dataDir = Path.Combine(_doctorService.GetProjectRoot(), "data");

        var dates = Directory.GetFiles(dataDir, "????????.json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Where(name => name != "baseline" && name.Length == 8 && int.TryParse(name, out _))
            .OrderDescending()
            .ToList();

        return Ok(dates);
    }

    [HttpGet("data/{date}")]
    public async Task<IActionResult> GetDataByDate(string date)
    {
        string dataDir = Path.Combine(_doctorService.GetProjectRoot(), "data");

        // Try exact date first
        string filePath = Path.Combine(dataDir, $"{date}.json");
        string actualDate = date;

        if (!System.IO.File.Exists(filePath))
        {
            // Find the closest previous date
            var available = Directory.GetFiles(dataDir, "????????.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(name => name != "baseline" && name.Length == 8 && int.TryParse(name, out _))
                .Where(name => string.Compare(name, date, StringComparison.Ordinal) <= 0)
                .OrderDescending()
                .FirstOrDefault();

            if (available == null)
            {
                return NotFound(new { message = "No data available for or before the requested date" });
            }

            filePath = Path.Combine(dataDir, $"{available}.json");
            actualDate = available;
        }

        var content = await System.IO.File.ReadAllTextAsync(filePath);
        Response.Headers["X-Actual-Date"] = actualDate;
        return Content($"{{\"actualDate\":\"{actualDate}\",\"data\":{content}}}", "application/json");
    }
}
