using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMES.Data;

namespace MMES.Controllers.Api;

[ApiController]
[Route("api/SmtApi/dashboard")]
public class SmtDashboardApiController : ControllerBase
{
    private readonly MMesDbContext _db;

    public SmtDashboardApiController(MMesDbContext db)
    {
        _db = db;
    }

    // =================================================================
    // PHASE 1: Pass Rate Summary from tb_production_full_view
    // Formula: PassRate = (TestResult == 'PASS') / (TestResult IN ('PASS', 'FAIL')) * 100
    // =================================================================
    [HttpGet("pass-rate-summary")]
    public async Task<IActionResult> GetPassRateSummary([FromQuery] DateTime? date)
    {
        try
        {
            var targetDate = date ?? DateTime.Today;

            var rawData = await _db.TbProductionFullViews
                .Where(x => x.TransactionDate.HasValue && x.TransactionDate.Value.Date == targetDate.Date
                            && (x.TestResult == "PASS" || x.TestResult == "FAIL"))
                .GroupBy(x => new { x.McType, x.McId })
                .Select(g => new
                {
                    McType = g.Key.McType,
                    McId = g.Key.McId,
                    TotalCount = g.Count(),
                    PassCount = g.Count(x => x.TestResult == "PASS")
                })
                .ToListAsync();

            double aoiRate = 0, spiRate = 0, moiRate = 0;

            var aoiItems = rawData.Where(x => string.Equals(x.McType, "AOI", StringComparison.OrdinalIgnoreCase)).ToList();
            var spiItems = rawData.Where(x => string.Equals(x.McType, "SPI", StringComparison.OrdinalIgnoreCase)).ToList();
            var moiItems = rawData.Where(x => string.Equals(x.McType, "MOI", StringComparison.OrdinalIgnoreCase)).ToList();

            if (aoiItems.Sum(x => x.TotalCount) > 0)
                aoiRate = Math.Round((double)aoiItems.Sum(x => x.PassCount) / aoiItems.Sum(x => x.TotalCount) * 100.0, 2);

            if (spiItems.Sum(x => x.TotalCount) > 0)
                spiRate = Math.Round((double)spiItems.Sum(x => x.PassCount) / spiItems.Sum(x => x.TotalCount) * 100.0, 2);

            if (moiItems.Sum(x => x.TotalCount) > 0)
                moiRate = Math.Round((double)moiItems.Sum(x => x.PassCount) / moiItems.Sum(x => x.TotalCount) * 100.0, 2);

            var spiByLine = spiItems.Select(x => new {
                Line = x.McId,
                Total = x.TotalCount,
                Pass = x.PassCount,
                PassRate = x.TotalCount > 0 ? Math.Round((double)x.PassCount / x.TotalCount * 100.0, 2) : 0
            }).ToList();

            var moiByLine = moiItems.Select(x => new {
                Line = x.McId,
                Total = x.TotalCount,
                Pass = x.PassCount,
                PassRate = x.TotalCount > 0 ? Math.Round((double)x.PassCount / x.TotalCount * 100.0, 2) : 0
            }).ToList();

            var aoiByLine = aoiItems.Select(x => new {
                Line = x.McId,
                Total = x.TotalCount,
                Pass = x.PassCount,
                PassRate = x.TotalCount > 0 ? Math.Round((double)x.PassCount / x.TotalCount * 100.0, 2) : 0
            }).ToList();

            return Ok(new
            {
                success = true,
                date = targetDate.ToString("yyyy-MM-dd"),
                aoiPassRate = aoiRate,
                spiPassRate = spiRate,
                moiPassRate = moiRate,
                spiByLine = spiByLine,
                moiByLine = moiByLine,
                aoiByLine = aoiByLine
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =================================================================
    // PHASE 1: Pickup Loss Summary from tb_pickup_miss
    // =================================================================
    [HttpGet("pickup-loss-summary")]
    public async Task<IActionResult> GetPickupLossSummary([FromQuery] DateTime? date)
    {
        try
        {
            var targetDate = DateOnly.FromDateTime(date ?? DateTime.Today);

            var totalRecords = await _db.TbPickupMisses
                .Where(p => p.ProdDate == targetDate && p.TimeSlot == "TOTAL" && p.PickupRate > 0)
                .Select(p => new
                {
                    Line = p.LineCode,
                    Machine = p.MachineName,
                    PickupRate = p.PickupRate,
                    PickupLossRate = p.PickupLossRate
                })
                .ToListAsync();

            double overallLossRate = totalRecords.Count > 0
                ? Math.Round((double)totalRecords.Average(x => x.PickupLossRate), 2)
                : 0.0;

            var lineSummary = totalRecords
                .GroupBy(x => x.Line)
                .Select(g => new
                {
                    LineCode = g.Key,
                    AverageLossRate = Math.Round((double)g.Average(x => x.PickupLossRate), 2),
                    MachineCount = g.Count(),
                    Machines = g.Select(m => new
                    {
                        m.Machine,
                        m.PickupRate,
                        m.PickupLossRate
                    }).ToList()
                })
                .ToList();

            return Ok(new
            {
                success = true,
                date = targetDate.ToString("yyyy-MM-dd"),
                overallPickupLossRate = overallLossRate,
                lines = lineSummary
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =================================================================
    // PHASE 1: Pickup Loss 7-Day Daily Trend from tb_pickup_miss
    // =================================================================
    [HttpGet("pickup-loss-daily-trend")]
    public async Task<IActionResult> GetPickupLossDailyTrend([FromQuery] DateTime? date, [FromQuery] double targetLimit = 0.5)
    {
        try
        {
            var endDate = DateOnly.FromDateTime(date ?? DateTime.Today);
            var startDate = endDate.AddDays(-6);

            var dbData = await _db.TbPickupMisses
                .Where(p => p.ProdDate >= startDate && p.ProdDate <= endDate && p.TimeSlot == "TOTAL" && p.PickupRate > 0)
                .GroupBy(p => p.ProdDate)
                .Select(g => new
                {
                    Date = g.Key,
                    AvgLossRate = Math.Round((double)g.Average(x => x.PickupLossRate), 3)
                })
                .ToListAsync();

            var dict = dbData.ToDictionary(x => x.Date, x => x.AvgLossRate);

            var labels = new List<string>();
            var dataPoints = new List<double>();
            var targetPoints = new List<double>();

            for (var d = startDate; d <= endDate; d = d.AddDays(1))
            {
                labels.Add($"{d.Day:D2}/{d.Month:D2}");
                var val = dict.ContainsKey(d) ? dict[d] : 0.0;
                dataPoints.Add(val);
                targetPoints.Add(targetLimit);
            }

            return Ok(new
            {
                success = true,
                targetLimit = targetLimit,
                labels = labels,
                data = dataPoints,
                targetData = targetPoints
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =================================================================
    // Phase 1 Mapping Dictionary: LineCode <-> AOI Machine (McId) - REAR (R) SIDE ONLY
    // =================================================================
    private static readonly Dictionary<string, string> McIdToLineMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "AOI_11_R2", "HM_11_R" },
        { "AOI_12_R2", "HM_12_R" },
        { "AOI_21_R2", "HM_21_R" },
        { "AOI_22_R2", "HM_22_R" },
        { "AOI_31_R2", "HM_31_R" },
        { "AOI_32_R2", "HM_32_R" }
    };

    private static readonly Dictionary<string, string[]> SummaryLineChildren = new(StringComparer.OrdinalIgnoreCase)
    {
        { "HM_1R", new[] { "HM_11_R", "HM_12_R" } },
        { "HM_2R", new[] { "HM_21_R", "HM_22_R" } },
        { "HM_3R", new[] { "HM_31_R", "HM_32_R" } }
    };

    private static string NormalizeMcIdToLineCode(string? rawMcId)
    {
        if (string.IsNullOrWhiteSpace(rawMcId)) return rawMcId ?? "";
        var trimmed = rawMcId.Trim();
        if (McIdToLineMap.TryGetValue(trimmed, out var mappedLine))
        {
            return mappedLine;
        }
        if (trimmed.StartsWith("AOI_", StringComparison.OrdinalIgnoreCase) && trimmed.Contains("_R", StringComparison.OrdinalIgnoreCase))
        {
            var result = "HM_" + trimmed.Substring(4);
            if (result.EndsWith("2"))
            {
                result = result.Substring(0, result.Length - 1);
            }
            return result;
        }
        return trimmed;
    }

    // =================================================================
    // PHASE 1: Realtime Total Output & Line Targets
    // Source: tb_smt_line_target (Targets) JOIN tb_production_full_view (Actuals - Rear R Only)
    // =================================================================
    [HttpGet("total-output-summary")]
    public async Task<IActionResult> GetTotalOutputSummary([FromQuery] DateTime? date)
    {
        try
        {
            var targetDate = date ?? DateTime.Today;
            var targetDateOnly = DateOnly.FromDateTime(targetDate);

            // 1. Fetch Target Plan from tb_smt_line_target (LATEST updated/inserted row per LineCode for target date)
            var rawTargets = await _db.TbSmtLineTargets
                .Where(t => t.ProdDate == targetDateOnly)
                .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            var dbTargets = rawTargets
                .GroupBy(t => t.LineCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            var shiftStart = targetDate.Date.AddHours(8);
            var shiftEnd = shiftStart.AddDays(1);

            // 2. Fetch Actual Outputs from tb_production_full_view (ONLY AOI Rear R machines: shift >= 08:00 AM, exclude BOTH TestResult and UserResult FAIL)
            var actualCountsRaw = await _db.TbProductionFullViews
                .Where(x => x.TransactionDate.HasValue
                            && x.TransactionDate.Value >= shiftStart
                            && x.TransactionDate.Value < shiftEnd
                            && !string.IsNullOrEmpty(x.McId)
                            && x.McId.Contains("_R")
                            && (x.TestResult != "FAIL" || x.UserResult != "FAIL"))
                .Select(x => new { McId = x.McId!, Pid = x.Pid })
                .ToListAsync();

            var actualDict = actualCountsRaw
                .Where(x => McIdToLineMap.ContainsKey(x.McId.Trim()) || x.McId.Contains("_R", StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => NormalizeMcIdToLineCode(x.McId), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Where(x => !string.IsNullOrWhiteSpace(x.Pid))
                          .Select(x => x.Pid!.Trim())
                          .Distinct(StringComparer.OrdinalIgnoreCase)
                          .Count(),
                    StringComparer.OrdinalIgnoreCase);

            // 3. Build combined output list for REAR (R) line targets only
            var lineDetails = new List<object>();

            // Helper to get actual output for a line code (direct or sum of child lanes for summary lines)
            int GetActualCount(string lineCode)
            {
                var code = lineCode.Trim();
                if (actualDict.TryGetValue(code, out int count))
                {
                    return count;
                }
                if (SummaryLineChildren.TryGetValue(code, out var children))
                {
                    int sum = 0;
                    foreach (var child in children)
                    {
                        if (actualDict.TryGetValue(child, out int childCount))
                        {
                            sum += childCount;
                        }
                    }
                    return sum;
                }
                return 0;
            }

            var rearTargets = dbTargets
                .Where(t => t.LineCode.EndsWith("R", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (rearTargets.Any())
            {
                foreach (var tgt in rearTargets)
                {
                    int actual = GetActualCount(tgt.LineCode);
                    double pct = tgt.TargetQty > 0 ? Math.Round((double)actual / tgt.TargetQty * 100.0, 1) : 0.0;

                    lineDetails.Add(new
                    {
                        lineCode = tgt.LineCode,
                        target = tgt.TargetQty,
                        actual = actual,
                        pct = pct,
                        isSummary = tgt.LineCode.Length <= 5,
                        isRear = true
                    });
                }
            }
            else
            {
                foreach (var kvp in actualDict.Where(k => k.Key.EndsWith("R", StringComparison.OrdinalIgnoreCase)))
                {
                    lineDetails.Add(new
                    {
                        lineCode = kvp.Key,
                        target = 0,
                        actual = kvp.Value,
                        pct = 0.0,
                        isSummary = kvp.Key.Length <= 5,
                        isRear = true
                    });
                }
            }

            // Calculate totals per main line (Line 1, Line 2, Line 3) based on REAR (R) side
            int GetTargetFor(string code) => dbTargets.FirstOrDefault(t => string.Equals(t.LineCode, code, StringComparison.OrdinalIgnoreCase))?.TargetQty ?? 0;

            int line1Target = GetTargetFor("HM_1R");
            if (line1Target == 0) line1Target = GetTargetFor("HM_11_R") + GetTargetFor("HM_12_R");

            int line2Target = GetTargetFor("HM_2R");
            if (line2Target == 0) line2Target = GetTargetFor("HM_21_R") + GetTargetFor("HM_22_R");

            int line3Target = GetTargetFor("HM_3R");
            if (line3Target == 0) line3Target = GetTargetFor("HM_31_R") + GetTargetFor("HM_32_R");

            int line1Actual = GetActualCount("HM_1R");
            int line2Actual = GetActualCount("HM_2R");
            int line3Actual = GetActualCount("HM_3R");

            int grandTotalActual = line1Actual + line2Actual + line3Actual;
            int grandTotalTarget = line1Target + line2Target + line3Target;

            double line1Pct = line1Target > 0 ? Math.Round((double)line1Actual / line1Target * 100.0, 1) : 0.0;
            double line2Pct = line2Target > 0 ? Math.Round((double)line2Actual / line2Target * 100.0, 1) : 0.0;
            double line3Pct = line3Target > 0 ? Math.Round((double)line3Actual / line3Target * 100.0, 1) : 0.0;

            var activeLinePcts = new List<double>();
            if (line1Target > 0) activeLinePcts.Add(line1Pct);
            if (line2Target > 0) activeLinePcts.Add(line2Pct);
            if (line3Target > 0) activeLinePcts.Add(line3Pct);

            double avgAchievePct = activeLinePcts.Any()
                ? Math.Round(activeLinePcts.Average(), 1)
                : (grandTotalTarget > 0 ? Math.Round((double)grandTotalActual / grandTotalTarget * 100.0, 1) : 0.0);

            return Ok(new
            {
                success = true,
                date = targetDate.ToString("yyyy-MM-dd"),
                totalOutput = grandTotalActual,
                totalTarget = grandTotalTarget,
                achievePct = avgAchievePct,
                line1 = new { target = line1Target, actual = line1Actual, pct = line1Pct },
                line2 = new { target = line2Target, actual = line2Actual, pct = line2Pct },
                line3 = new { target = line3Target, actual = line3Actual, pct = line3Pct },
                lines = lineDetails
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =================================================================
    // PHASE 1: History Trends (Output Trend Last 7 Days - Rear R Only)
    // Source: tb_production_full_view (08:00 AM Shift) & tb_smt_line_target
    // =================================================================
    [HttpGet("history-trends")]
    public async Task<IActionResult> GetHistoryTrends([FromQuery] DateTime? date)
    {
        try
        {
            var now = DateTime.Now;
            var baseDate = date ?? (now.TimeOfDay < TimeSpan.FromHours(8) ? DateTime.Today.AddDays(-1) : DateTime.Today);

            var overallStart = baseDate.AddDays(-6).Date.AddHours(8);
            var overallEnd = baseDate.Date.AddDays(1).AddHours(8);

            // 1. Fetch Actual Output Records for 7-day range (Rear machines only, exclude double FAIL)
            var rawActuals = await _db.TbProductionFullViews
                .Where(x => x.TransactionDate.HasValue
                            && x.TransactionDate.Value >= overallStart
                            && x.TransactionDate.Value < overallEnd
                            && !string.IsNullOrEmpty(x.McId)
                            && x.McId.Contains("_R")
                            && (x.TestResult != "FAIL" || x.UserResult != "FAIL")
                            && !string.IsNullOrWhiteSpace(x.Pid))
                .Select(x => new
                {
                    TxDate = x.TransactionDate!.Value,
                    Pid = x.Pid!.Trim()
                })
                .ToListAsync();

            // 2. Fetch Line Targets for 7-day range from tb_smt_line_target
            var startDateOnly = DateOnly.FromDateTime(baseDate.AddDays(-6));
            var endDateOnly = DateOnly.FromDateTime(baseDate);

            var rawTargets = await _db.TbSmtLineTargets
                .Where(t => t.ProdDate >= startDateOnly && t.ProdDate <= endDateOnly)
                .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            // Find default fallback target from today's targets
            var todayDateOnly = DateOnly.FromDateTime(baseDate);
            var todayTargets = rawTargets
                .Where(t => t.ProdDate == todayDateOnly)
                .GroupBy(t => t.LineCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            int GetTodayTargetFor(string code) => todayTargets.FirstOrDefault(t => string.Equals(t.LineCode, code, StringComparison.OrdinalIgnoreCase))?.TargetQty ?? 0;
            int todayLine1 = GetTodayTargetFor("HM_1R"); if (todayLine1 == 0) todayLine1 = GetTodayTargetFor("HM_11_R") + GetTodayTargetFor("HM_12_R");
            int todayLine2 = GetTodayTargetFor("HM_2R"); if (todayLine2 == 0) todayLine2 = GetTodayTargetFor("HM_21_R") + GetTodayTargetFor("HM_22_R");
            int todayLine3 = GetTodayTargetFor("HM_3R"); if (todayLine3 == 0) todayLine3 = GetTodayTargetFor("HM_31_R") + GetTodayTargetFor("HM_32_R");
            int defaultFallbackTarget = (todayLine1 + todayLine2 + todayLine3) > 0 ? (todayLine1 + todayLine2 + todayLine3) : 10691;

            // 3. Construct 7-day daily data points (from T-6 to T)
            var output7Days = new List<object>();

            for (int i = 6; i >= 0; i--)
            {
                var curDate = baseDate.AddDays(-i);
                var curDateOnly = DateOnly.FromDateTime(curDate);

                var dayShiftStart = curDate.Date.AddHours(8);
                var dayShiftEnd = dayShiftStart.AddDays(1);

                // Count unique PIDs in dayShiftStart .. dayShiftEnd
                var dayActual = rawActuals
                    .Where(x => x.TxDate >= dayShiftStart && x.TxDate < dayShiftEnd)
                    .Select(x => x.Pid)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                // Compute day target from tb_smt_line_target
                var dayTargets = rawTargets
                    .Where(t => t.ProdDate == curDateOnly)
                    .GroupBy(t => t.LineCode.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                int GetDayTargetFor(string code) => dayTargets.FirstOrDefault(t => string.Equals(t.LineCode, code, StringComparison.OrdinalIgnoreCase))?.TargetQty ?? 0;
                int dayLine1 = GetDayTargetFor("HM_1R"); if (dayLine1 == 0) dayLine1 = GetDayTargetFor("HM_11_R") + GetDayTargetFor("HM_12_R");
                int dayLine2 = GetDayTargetFor("HM_2R"); if (dayLine2 == 0) dayLine2 = GetDayTargetFor("HM_21_R") + GetDayTargetFor("HM_22_R");
                int dayLine3 = GetDayTargetFor("HM_3R"); if (dayLine3 == 0) dayLine3 = GetDayTargetFor("HM_31_R") + GetDayTargetFor("HM_32_R");

                int dayTargetSum = dayLine1 + dayLine2 + dayLine3;
                if (dayTargetSum == 0)
                {
                    dayTargetSum = defaultFallbackTarget;
                }

                output7Days.Add(new
                {
                    date = curDate.ToString("dd/MM"),
                    year = curDate.Year,
                    month = curDate.Month,
                    day = curDate.Day,
                    output = dayActual,
                    target = dayTargetSum
                });
            }

            return Ok(new
            {
                success = true,
                output7Days = output7Days,
                upph7Days = new List<object>(),
                pickupLoss7Days = new List<object>(),
                idleTime7Days = new List<object>(),
                defect7Days = new List<object>(),
                oee7Days = new List<object>()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}

