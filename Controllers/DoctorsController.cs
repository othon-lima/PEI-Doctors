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
        var (filePath, actualDate) = ResolveDataFile(date);
        if (filePath == null)
            return NotFound(new { message = "No data available for or before the requested date" });

        var content = await System.IO.File.ReadAllTextAsync(filePath);
        Response.Headers["X-Actual-Date"] = actualDate;
        return Content($"{{\"actualDate\":\"{actualDate}\",\"data\":{content}}}", "application/json");
    }

    [HttpGet("compare/{date1}/{date2}")]
    public async Task<IActionResult> CompareDates(string date1, string date2)
    {
        var (file1, actual1) = ResolveDataFile(date1);
        var (file2, actual2) = ResolveDataFile(date2);

        if (file1 == null || file2 == null)
            return NotFound(new { message = "No data available for one or both dates" });

        var oldJson = await System.IO.File.ReadAllTextAsync(file1);
        var newJson = await System.IO.File.ReadAllTextAsync(file2);
        var result = _doctorService.GetStructuredDiffs(oldJson, newJson);

        return Ok(new { fromDate = actual1, toDate = actual2, result });
    }

    private (string? filePath, string? actualDate) ResolveDataFile(string date)
    {
        string dataDir = Path.Combine(_doctorService.GetProjectRoot(), "data");
        string filePath = Path.Combine(dataDir, $"{date}.json");

        if (System.IO.File.Exists(filePath))
            return (filePath, date);

        var available = Directory.GetFiles(dataDir, "????????.json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Where(name => name != "baseline" && name.Length == 8 && int.TryParse(name, out _))
            .Where(name => string.Compare(name, date, StringComparison.Ordinal) <= 0)
            .OrderDescending()
            .FirstOrDefault();

        if (available == null)
            return (null, null);

        return (Path.Combine(dataDir, $"{available}.json"), available);
    }
}
