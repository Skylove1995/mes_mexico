namespace MMES.Models;

public record OqcScanoutCell(
    int Qty,
    int Overage,
    int Uph,
    double EfficiencyPct
);

public record OqcScanoutRow(
    string Line,
    string ModelSuffix,
    List<OqcScanoutCell?> Slots,
    OqcScanoutCell NightTotal,
    OqcScanoutCell DayTotal,
    OqcScanoutCell GrandTotal
);

public record OqcScanoutSummary(
    int TotalQty,
    int TotalOverageQty,
    int AvgUph,
    int PeakUph,
    string PeakUphSlot,
    double EfficiencyPct,
    int ActiveLines,
    string TopModelSuffix,
    int TopModelQty
);

public record OqcScanoutTrendPoint(
    string HourLabel,
    int NightQty,
    int DayQty,
    int ActualUph,
    int TargetUph,
    int OverageQty
);

public record OqcScanoutResponse(
    string Date,
    string ShiftStart,
    string ShiftEnd,
    OqcScanoutSummary Summary,
    List<OqcScanoutRow> Rows,
    List<OqcScanoutTrendPoint> Trend
);
