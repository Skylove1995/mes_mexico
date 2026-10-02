using Microsoft.EntityFrameworkCore;
using MMES.Data;
using MMES.Models;

namespace MMES.Services;

public static class OqcScanoutCalculator
{
    private const int TargetUphStandard = 416; // Standard ~416 UPH across factory (Target ~10,000 PCS per 24h)
    private const int SlotHours = 2;
    private const int SlotTargetStandard = 833; // ~833 PCS per 2-hour slot

    private static readonly string[] SlotLabels = new[]
    {
        "20-22", "22-00", "00-02", "02-04", "04-06", "06-08",
        "08-10", "10-12", "12-14", "14-16", "16-18", "18-20"
    };

    public static async Task<OqcScanoutResponse> CalculateAsync(MMesDbContext context, DateOnly date)
    {
        var shiftStart = date.AddDays(-1).ToDateTime(new TimeOnly(20, 0, 0));
        var shiftEnd = date.ToDateTime(new TimeOnly(20, 0, 0));

        var scanoutRecords = await FetchRecordsAsync(context, shiftStart, shiftEnd);

        // Automatic fallback: if selected date has 0 records, find latest available scan date
        if (scanoutRecords.Count == 0)
        {
            var maxScanAt = await context.TbScanOuts
                .AsNoTracking()
                .Where(s => s.ScanAt != null)
                .Select(s => (DateTime?)s.ScanAt)
                .MaxAsync();

            if (maxScanAt.HasValue)
            {
                var latestScanDate = DateOnly.FromDateTime(maxScanAt.Value);
                // If maxScanAt hour < 20, shift belongs to current day, else next day
                if (maxScanAt.Value.Hour >= 20)
                {
                    latestScanDate = latestScanDate.AddDays(1);
                }

                date = latestScanDate;
                shiftStart = date.AddDays(-1).ToDateTime(new TimeOnly(20, 0, 0));
                shiftEnd = date.ToDateTime(new TimeOnly(20, 0, 0));

                scanoutRecords = await FetchRecordsAsync(context, shiftStart, shiftEnd);
            }
        }

        // Aggregate by (Line, ModelSuffix, SlotIndex)
        var buckets = new Dictionary<(string Line, string ModelSuffix, int SlotIndex), int>();

        foreach (var r in scanoutRecords)
        {
            var slotIdx = GetSlotIndex(r.ScanAt, shiftStart);
            if (slotIdx < 0 || slotIdx >= 12) continue;

            var key = (r.Line, r.ModelSuffix, slotIdx);
            buckets.TryGetValue(key, out var currentQty);
            buckets[key] = currentQty + r.Qty;
        }

        // Get distinct Lines & ModelSuffixes
        var lineModelPairs = scanoutRecords
            .Select(r => (r.Line, r.ModelSuffix))
            .Distinct()
            .OrderBy(p => p.Line, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.ModelSuffix, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = new List<OqcScanoutRow>();

        // Overall aggregators
        var slotTotals = new int[12];
        var totalQtyAll = 0;
        var activeLinesSet = new HashSet<string>();
        var modelQtyMap = new Dictionary<string, int>();

        foreach (var (line, modelSuffix) in lineModelPairs)
        {
            activeLinesSet.Add(line);
            var slots = new List<OqcScanoutCell?>();
            var nightQty = 0;
            var dayQty = 0;

            for (var i = 0; i < 12; i++)
            {
                if (buckets.TryGetValue((line, modelSuffix, i), out var qty) && qty > 0)
                {
                    var overage = qty - SlotTargetStandard;
                    var uph = (int)Math.Round((double)qty / SlotHours);
                    var eff = Math.Round((double)qty * 100.0 / SlotTargetStandard, 1);

                    slots.Add(new OqcScanoutCell(qty, overage, uph, eff));

                    if (i < 6) nightQty += qty;
                    else dayQty += qty;

                    slotTotals[i] += qty;
                    totalQtyAll += qty;

                    modelQtyMap.TryGetValue(modelSuffix, out var mQty);
                    modelQtyMap[modelSuffix] = mQty + qty;
                }
                else
                {
                    slots.Add(null);
                }
            }

            var nightCell = BuildAggregateCell(nightQty, 6 * SlotTargetStandard, 12);
            var dayCell = BuildAggregateCell(dayQty, 6 * SlotTargetStandard, 12);
            var grandCell = BuildAggregateCell(nightQty + dayQty, 12 * SlotTargetStandard, 24);

            rows.Add(new OqcScanoutRow(line, modelSuffix, slots, nightCell, dayCell, grandCell));
        }

        // Summary Calculations
        var peakSlotQty = 0;
        var peakSlotLabel = "N/A";
        for (var i = 0; i < 12; i++)
        {
            if (slotTotals[i] > peakSlotQty)
            {
                peakSlotQty = slotTotals[i];
                peakSlotLabel = SlotLabels[i];
            }
        }

        var peakUph = (int)Math.Round((double)peakSlotQty / SlotHours);
        var avgUphAll = (int)Math.Round((double)totalQtyAll / 24.0);
        var totalTargetStandard = Math.Max(1, lineModelPairs.Select(p => p.Line).Distinct().Count()) * 12 * SlotTargetStandard;
        var totalOverageAll = totalQtyAll - totalTargetStandard;
        var overallEff = totalTargetStandard > 0 ? Math.Round((double)totalQtyAll * 100.0 / totalTargetStandard, 1) : 0;

        var topModel = modelQtyMap.OrderByDescending(kv => kv.Value).FirstOrDefault();
        var topModelSuffix = string.IsNullOrEmpty(topModel.Key) ? "N/A" : topModel.Key;

        var summary = new OqcScanoutSummary(
            TotalQty: totalQtyAll,
            TotalOverageQty: totalOverageAll,
            AvgUph: avgUphAll,
            PeakUph: peakUph,
            PeakUphSlot: peakSlotLabel,
            EfficiencyPct: overallEff,
            ActiveLines: activeLinesSet.Count,
            TopModelSuffix: topModelSuffix,
            TopModelQty: topModel.Value
        );

        // Build 12-Slot Trend Points
        var trend = new List<OqcScanoutTrendPoint>();
        for (var i = 0; i < 12; i++)
        {
            var isNight = i < 6;
            var slotQty = slotTotals[i];
            var slotUph = (int)Math.Round((double)slotQty / SlotHours);
            var slotTarget = activeLinesSet.Count > 0 ? activeLinesSet.Count * SlotTargetStandard : SlotTargetStandard;
            var slotOverage = slotQty - slotTarget;

            trend.Add(new OqcScanoutTrendPoint(
                HourLabel: SlotLabels[i] + (isNight ? " (Night)" : " (Day)"),
                NightQty: isNight ? slotQty : 0,
                DayQty: isNight ? 0 : slotQty,
                ActualUph: slotUph,
                TargetUph: activeLinesSet.Count > 0 ? activeLinesSet.Count * TargetUphStandard : TargetUphStandard,
                OverageQty: slotOverage
            ));
        }

        return new OqcScanoutResponse(
            Date: date.ToString("yyyy-MM-dd"),
            ShiftStart: shiftStart.ToString("yyyy-MM-dd HH:mm:ss"),
            ShiftEnd: shiftEnd.ToString("yyyy-MM-dd HH:mm:ss"),
            Summary: summary,
            Rows: rows,
            Trend: trend
        );
    }

    private static async Task<List<ScanoutRecordInternal>> FetchRecordsAsync(MMesDbContext context, DateTime shiftStart, DateTime shiftEnd)
    {
        var lineMappings = await context.TbScanoutLines
            .AsNoTracking()
            .Where(l => !string.IsNullOrEmpty(l.Ip))
            .ToListAsync();

        var ipToLineMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in lineMappings)
        {
            var ipKey = m.Ip!.Trim();
            if (!ipToLineMap.ContainsKey(ipKey) && !string.IsNullOrWhiteSpace(m.LineNo))
            {
                ipToLineMap[ipKey] = m.LineNo.Trim();
            }
        }

        var raw = await context.TbScanOuts
            .AsNoTracking()
            .Where(s => s.ScanAt >= shiftStart && s.ScanAt < shiftEnd)
            .Select(s => new
            {
                s.ClientId,
                s.PartNo,
                ScanAt = s.ScanAt!.Value,
                Qty = s.Qty ?? 1
            })
            .ToListAsync();

        return raw.Select(s =>
        {
            var rawIp = string.IsNullOrWhiteSpace(s.ClientId) ? "LINE UNKNOWN" : s.ClientId.Trim();
            var lineName = ipToLineMap.TryGetValue(rawIp, out var mappedLine) ? mappedLine : rawIp;

            return new ScanoutRecordInternal(
                Line: lineName,
                ModelSuffix: string.IsNullOrWhiteSpace(s.PartNo) ? "N/A" : s.PartNo.Trim(),
                ScanAt: s.ScanAt,
                Qty: s.Qty
            );
        }).ToList();
    }

    private static int GetSlotIndex(DateTime scanAt, DateTime shiftStart)
    {
        var diff = scanAt - shiftStart;
        if (diff.TotalHours < 0 || diff.TotalHours >= 24) return -1;
        return (int)(diff.TotalHours / 2);
    }

    private static OqcScanoutCell BuildAggregateCell(int totalQty, int targetQty, int totalHours)
    {
        var overage = totalQty - targetQty;
        var uph = (int)Math.Round((double)totalQty / totalHours);
        var eff = targetQty > 0 ? Math.Round((double)totalQty * 100.0 / targetQty, 1) : 0;
        return new OqcScanoutCell(totalQty, overage, uph, eff);
    }

    private record ScanoutRecordInternal(string Line, string ModelSuffix, DateTime ScanAt, int Qty);
}
