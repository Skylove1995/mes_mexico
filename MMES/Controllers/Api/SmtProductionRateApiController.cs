using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMES.Data;
using MMES.Models;
using MMES.Services;

namespace MMES.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/smt/production-rate")]
public class SmtProductionRateApiController : ControllerBase
{
    private readonly MMesDbContext _context;

    public SmtProductionRateApiController(MMesDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ProductionRateResponse>> Get([FromQuery] string? date)
    {
        if (string.IsNullOrEmpty(date) || !DateOnly.TryParse(date, out var targetDate))
        {
            targetDate = DateOnly.FromDateTime(DateTime.Now);
        }

        var result = await ProductionRateCalculator.CalculateAsync(_context, targetDate);
        return Ok(result);
    }

    [HttpGet("monthly-trend")]
    public async Task<ActionResult<MonthlyTrendResponse>> GetMonthlyTrend([FromQuery] string? month, [FromQuery] string? type)
    {
        var machineType = string.IsNullOrEmpty(type) ? "SPI" : type.ToUpper();
        var result = await ProductionRateCalculator.CalculateMonthlyTrendAsync(_context, month ?? string.Empty, machineType);
        return Ok(result);
    }
}

