using Microsoft.EntityFrameworkCore;
using MMES.Data;
using MMES.Models;

namespace MMES.Services;

// Tinh du lieu Production Rate (Day shift 08:00-20:00, Night shift 20:00 hom truoc - 08:00) cho 1 ngay,
// gom ca 3 loai may SPI/MOI/AOI trong 1 lan goi.
public static class ProductionRateCalculator
{
    private static readonly string[] MachineTypes = { "SPI", "MOI", "AOI" };

    public static async Task<ProductionRateResponse> CalculateAsync(MMesDbContext context, DateOnly date)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue).AddHours(8);
        var dayEnd = date.ToDateTime(TimeOnly.MinValue).AddHours(20);
        var nightStart = date.AddDays(-1).ToDateTime(TimeOnly.MinValue).AddHours(20);

        // Bang may nho (~vai chuc dong), load 1 lan roi parse Line tu ma may (VD "AOI_21_F1" -> Line "2.1").
        var machines = await context.TbProductionMachines.AsNoTracking().ToListAsync();

        var machineMap = new Dictionary<int, (string McType, string Line)>();
        foreach (var m in machines)
        {
            var line = ParseLine(m.McId);
            if (line is not null && !string.IsNullOrEmpty(m.McType))
            {
                machineMap[m.Id] = (m.McType!, line);
            }
        }

        var lines = machineMap.Values
            .Select(v => v.Line)
            .Distinct()
            .OrderBy(l => l, StringComparer.Ordinal)
            .ToList();

        var rows = await context.TbProductionResults
            .Where(r => r.TransactionDate >= nightStart && r.TransactionDate < dayEnd)
            .Select(r => new { r.McId, r.TestResult, r.UserResult, r.TransactionDate })
            .ToListAsync();

        // Neu ngay duoc chon khong co du lieu, tu dong tim ngay moi nhat trong DB co du lieu san xuat
        if (rows.Count == 0)
        {
            var maxTransDate = await context.TbProductionResults
                .Select(r => (DateTime?)r.TransactionDate)
                .MaxAsync();

            if (maxTransDate.HasValue)
            {
                date = DateOnly.FromDateTime(maxTransDate.Value);
                dayStart = date.ToDateTime(TimeOnly.MinValue).AddHours(8);
                dayEnd = date.ToDateTime(TimeOnly.MinValue).AddHours(20);
                nightStart = date.AddDays(-1).ToDateTime(TimeOnly.MinValue).AddHours(20);

                rows = await context.TbProductionResults
                    .Where(r => r.TransactionDate >= nightStart && r.TransactionDate < dayEnd)
                    .Select(r => new { r.McId, r.TestResult, r.UserResult, r.TransactionDate })
                    .ToListAsync();
            }
        }

        var buckets = new Dictionary<(string McType, string Line, int Hour), (int Total, int UserPass)>();

        foreach (var r in rows)
        {
            if (!machineMap.TryGetValue(r.McId, out var info))
            {
                continue;
            }

            var key = (info.McType, info.Line, r.TransactionDate.Hour);
            buckets.TryGetValue(key, out var agg);
            agg.Total++;
            if (r.TestResult == "FAIL" && r.UserResult == "PASS")
            {
                agg.UserPass++;
            }

            buckets[key] = agg;
        }

        var dayHours = Enumerable.Range(8, 12).ToList();
        var nightHours = Enumerable.Range(20, 4).Concat(Enumerable.Range(0, 8)).ToList();
        var trendHours = nightHours.Concat(dayHours).ToList(); // chronological: nightStart -> dayEnd

        ProductionRateShiftTable BuildTable(string mcType, List<int> hours)
        {
            var hourLabels = hours.Select(h => $"{h:00}-{(h + 1) % 24:00}").ToList();
            var lineRows = lines.Select(line =>
            {
                var lineTotal = 0;
                var lineUserPass = 0;
                var cells = hours.Select(h =>
                {
                    if (buckets.TryGetValue((mcType, line, h), out var agg) && agg.Total > 0)
                    {
                        lineTotal += agg.Total;
                        lineUserPass += agg.UserPass;
                        var pct = Math.Round(agg.UserPass * 100.0 / agg.Total, 1);
                        return new ProductionRateCell(agg.Total, agg.UserPass, pct);
                    }

                    return null;
                }).ToList();

                var linePct = lineTotal > 0 ? Math.Round(lineUserPass * 100.0 / lineTotal, 1) : 0;
                var totalCell = new ProductionRateCell(lineTotal, lineUserPass, linePct);

                return new ProductionRateLineRow(line, cells, totalCell);
            }).ToList();

            return new ProductionRateShiftTable(hourLabels, lineRows);
        }

        // Gom tat ca line lai theo 1 khung gio (hoac ca ca) de ra so tong.
        ProductionRateShiftSummary BuildSummary(string mcType, IEnumerable<int> hours)
        {
            var total = 0;
            var userPass = 0;
            foreach (var line in lines)
            {
                foreach (var h in hours)
                {
                    if (buckets.TryGetValue((mcType, line, h), out var agg))
                    {
                        total += agg.Total;
                        userPass += agg.UserPass;
                    }
                }
            }

            var pct = total > 0 ? Math.Round(userPass * 100.0 / total, 1) : 0;
            return new ProductionRateShiftSummary(total, userPass, pct);
        }

        // Tinh KPI chi tiet cho tung Line
        List<ProductionRateLineKpiSummary> BuildLineSummaries(string mcType)
        {
            var result = new List<ProductionRateLineKpiSummary>();
            foreach (var line in lines)
            {
                int dayTot = 0, dayPass = 0;
                foreach (var h in dayHours)
                {
                    if (buckets.TryGetValue((mcType, line, h), out var agg))
                    {
                        dayTot += agg.Total;
                        dayPass += agg.UserPass;
                    }
                }
                var dayPct = dayTot > 0 ? Math.Round(dayPass * 100.0 / dayTot, 1) : 0;

                int nightTot = 0, nightPass = 0;
                foreach (var h in nightHours)
                {
                    if (buckets.TryGetValue((mcType, line, h), out var agg))
                    {
                        nightTot += agg.Total;
                        nightPass += agg.UserPass;
                    }
                }
                var nightPct = nightTot > 0 ? Math.Round(nightPass * 100.0 / nightTot, 1) : 0;

                int overTot = dayTot + nightTot;
                int overPass = dayPass + nightPass;
                var overPct = overTot > 0 ? Math.Round(overPass * 100.0 / overTot, 1) : 0;

                result.Add(new ProductionRateLineKpiSummary(
                    line,
                    new ProductionRateShiftSummary(dayTot, dayPass, dayPct),
                    new ProductionRateShiftSummary(nightTot, nightPass, nightPct),
                    new ProductionRateShiftSummary(overTot, overPass, overPct)
                ));
            }
            return result;
        }

        List<ProductionRateTrendPoint> BuildTrend(string mcType)
        {
            return trendHours.Select(h =>
            {
                var total = 0;
                var userPass = 0;
                foreach (var line in lines)
                {
                    if (buckets.TryGetValue((mcType, line, h), out var agg))
                    {
                        total += agg.Total;
                        userPass += agg.UserPass;
                    }
                }

                var pct = total > 0 ? Math.Round(userPass * 100.0 / total, 1) : 0;
                var label = $"{h:00}-{(h + 1) % 24:00}";
                return new ProductionRateTrendPoint(label, total, userPass, pct);
            }).ToList();
        }

        var machineTypeData = MachineTypes.ToDictionary(
            t => t,
            t => new ProductionRateMachineTypeData(
                BuildTable(t, dayHours),
                BuildTable(t, nightHours),
                BuildSummary(t, dayHours),
                BuildSummary(t, nightHours),
                BuildSummary(t, trendHours),
                BuildLineSummaries(t),
                BuildTrend(t)));

        return new ProductionRateResponse(date.ToString("yyyy-MM-dd"), machineTypeData);
    }

    public static async Task<MonthlyTrendResponse> CalculateMonthlyTrendAsync(MMesDbContext context, string monthStr, string machineType)
    {
        if (string.IsNullOrEmpty(monthStr) || !DateTime.TryParseExact(monthStr + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var firstDayOfMonth))
        {
            firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            monthStr = firstDayOfMonth.ToString("yyyy-MM");
        }

        int year = firstDayOfMonth.Year;
        int month = firstDayOfMonth.Month;
        int daysInMonth = DateTime.DaysInMonth(year, month);

        // Timeline tu 20:00 ngay cuoi thang truoc den 20:00 ngay cuoi thang nay
        var monthStartUtc = firstDayOfMonth.AddDays(-1).AddHours(20);
        var monthEndUtc = firstDayOfMonth.AddDays(daysInMonth - 1).AddHours(20);

        var machines = await context.TbProductionMachines.AsNoTracking().ToListAsync();
        var validMachineIds = new HashSet<int>();
        foreach (var m in machines)
        {
            if (!string.IsNullOrEmpty(m.McType) && m.McType.Equals(machineType, StringComparison.OrdinalIgnoreCase))
            {
                validMachineIds.Add(m.Id);
            }
        }

        var rows = await context.TbProductionResults
            .Where(r => validMachineIds.Contains(r.McId) && r.TransactionDate >= monthStartUtc && r.TransactionDate < monthEndUtc)
            .Select(r => new { r.TestResult, r.UserResult, r.TransactionDate })
            .ToListAsync();

        var points = new List<MonthlyTrendPoint>();

        for (int day = 1; day <= daysInMonth; day++)
        {
            var dayTarget = new DateTime(year, month, day);
            var shiftStart = dayTarget.AddDays(-1).AddHours(20);
            var shiftEnd = dayTarget.AddHours(20);

            var dayRows = rows.Where(r => r.TransactionDate >= shiftStart && r.TransactionDate < shiftEnd).ToList();
            int total = dayRows.Count;
            int userPass = dayRows.Count(r => r.TestResult == "FAIL" && r.UserResult == "PASS");
            double pct = total > 0 ? Math.Round(userPass * 100.0 / total, 1) : 0;

            points.Add(new MonthlyTrendPoint(day, dayTarget.ToString("yyyy-MM-dd"), total, userPass, pct));
        }

        return new MonthlyTrendResponse(monthStr, machineType, points);
    }

    // "AOI_21_F1" -> "2.1". Tra ve null neu khong dung dinh dang mong doi.
    private static string? ParseLine(string machineCode)
    {
        var parts = machineCode.Split('_');
        if (parts.Length < 2 || parts[1].Length != 2 || !char.IsDigit(parts[1][0]) || !char.IsDigit(parts[1][1]))
        {
            return null;
        }

        return $"{parts[1][0]}.{parts[1][1]}";
    }
}

