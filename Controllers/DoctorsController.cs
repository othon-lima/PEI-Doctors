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
            await _doctorService.RunScrapeAsync();
            return Ok(new { message = "Scrape completed successfully" });
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
}
