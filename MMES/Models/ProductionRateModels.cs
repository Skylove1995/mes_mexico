namespace MMES.Models;

public record ProductionRateCell(int Total, int UserPass, double Pct);

public record ProductionRateLineRow(string Line, List<ProductionRateCell?> Hours, ProductionRateCell TotalCell);

public record ProductionRateShiftTable(List<string> HourLabels, List<ProductionRateLineRow> Lines);

public record ProductionRateShiftSummary(int Total, int UserPass, double Pct);

public record ProductionRateLineKpiSummary(
    string Line,
    ProductionRateShiftSummary DaySummary,
    ProductionRateShiftSummary NightSummary,
    ProductionRateShiftSummary OverallSummary);

public record ProductionRateTrendPoint(string HourLabel, int Total, int UserPass, double Pct);

public record ProductionRateMachineTypeData(
    ProductionRateShiftTable Day,
    ProductionRateShiftTable Night,
    ProductionRateShiftSummary DaySummary,
    ProductionRateShiftSummary NightSummary,
    ProductionRateShiftSummary OverallSummary,
    List<ProductionRateLineKpiSummary> LineSummaries,
    List<ProductionRateTrendPoint> Trend);

public record ProductionRateResponse(string Date, Dictionary<string, ProductionRateMachineTypeData> MachineTypes);

public record MonthlyTrendPoint(int Day, string DateStr, int Total, int UserPass, double Pct);

public record MonthlyTrendResponse(string Month, string MachineType, List<MonthlyTrendPoint> Points);

