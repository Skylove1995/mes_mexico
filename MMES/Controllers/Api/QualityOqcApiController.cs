using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMES.Data;
using MMES.Models;
using MMES.Services;

namespace MMES.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/quality/oqc-scanout")]
public class QualityOqcApiController : ControllerBase
{
    private readonly MMesDbContext _context;

    public QualityOqcApiController(MMesDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<OqcScanoutResponse>> Get([FromQuery] string? date)
    {
        if (string.IsNullOrEmpty(date) || !DateOnly.TryParse(date, out var targetDate))
        {
            targetDate = DateOnly.FromDateTime(DateTime.Now);
        }

        var result = await OqcScanoutCalculator.CalculateAsync(_context, targetDate);
        return Ok(result);
    }
}
