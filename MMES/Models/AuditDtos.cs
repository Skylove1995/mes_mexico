using System;
using System.Collections.Generic;

namespace MMES.Models;

public class AuditSessionDto
{
    public int Id { get; set; }
    public int? AuditTypeId { get; set; }
    public string? AuditTypeName { get; set; }
    public int? DeptId { get; set; }
    public string? DeptName { get; set; }
    public DateTime? AuditMonth { get; set; }
    public string? Shift { get; set; }
    public int? AuditorId { get; set; }
    public string? AuditorName { get; set; }
    public int? CheckedById { get; set; }
    public string? CheckedByName { get; set; }
    public DateTime? CheckedAt { get; set; }
    public int? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? StatusId { get; set; }
    public string? StatusName { get; set; }
    public decimal? TotalScore { get; set; }
    public decimal? TotalScoreTarget { get; set; }
    public decimal? ScorePercent { get; set; }
    public string? Grade { get; set; }
    public string? Note { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int TotalItems { get; set; }
    public int AnsweredItems { get; set; }
    public int IssueCount { get; set; }
}

public class CreateAuditSessionRequest
{
    public int AuditTypeId { get; set; }
    public int DeptId { get; set; }
    public DateTime AuditMonth { get; set; }
    public string Shift { get; set; } = "Day";
    public int? AuditorId { get; set; }
    public string? Note { get; set; }
}

public class AuditChecksheetItemDto
{
    public int Id { get; set; }
    public int? AuditTypeId { get; set; }
    public string? Category { get; set; }
    public int? SeqNo { get; set; }
    public string? AuditItem { get; set; }
    public string? AuditItemTranslate { get; set; }
    public decimal? MaxScore { get; set; }
    public string? Critical { get; set; }
    public string? TimeLimit { get; set; }
    public string? Active { get; set; }
    public int? ForDept { get; set; }
    public string? DeptName { get; set; }

    // Existing score in session (if conduct mode)
    public int? ResultId { get; set; }
    public decimal? Score { get; set; }
    public string? Judge { get; set; }
    public string? Note { get; set; }
    public int? IssueId { get; set; }
}

public class SubmitAuditConductRequest
{
    public int SessionId { get; set; }
    public string Action { get; set; } = "draft"; // "draft", "submit", "check", "approve"
    public string? Note { get; set; }
    public List<AuditItemScoreSubmission> Items { get; set; } = new();
}

public class AuditItemScoreSubmission
{
    public int AuditItemId { get; set; }
    public decimal? Score { get; set; } // 0, 2, 4, 6, 8, 10
    public string? Judge { get; set; } // "PASS", "FAIL", "PARTIAL"
    public string? Note { get; set; }
}

public class AuditActionDto
{
    public int Id { get; set; }
    public int AuditResultId { get; set; }
    public int SessionId { get; set; }
    public string? AuditTypeName { get; set; }
    public string? DeptName { get; set; }
    public string? Category { get; set; }
    public string? AuditItem { get; set; }
    public string? ActionType { get; set; }
    public string? Description { get; set; }
    public int? PicId { get; set; }
    public string? PicName { get; set; }
    public DateTime? DueDate { get; set; }
    public int? StatusId { get; set; }
    public string? StatusName { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<string> Evidences { get; set; } = new();
}

public class CreateAuditActionRequest
{
    public int AuditResultId { get; set; }
    public string? ActionType { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? PicId { get; set; }
    public DateTime? DueDate { get; set; }
    public List<string>? EvidenceUrls { get; set; }
}

public class UpdateAuditActionStatusRequest
{
    public int ActionId { get; set; }
    public int StatusId { get; set; }
    public string? Comment { get; set; }
    public List<string>? EvidenceUrls { get; set; }
}

public class AuditSummaryKPIsDto
{
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int DraftSessions { get; set; }
    public int PendingApprovalSessions { get; set; }
    public decimal AverageScorePercent { get; set; }
    public int TotalIssues { get; set; }
    public int OpenIssues { get; set; }
    public int RectifiedIssues { get; set; }
    public int ClosedIssues { get; set; }
    public List<DeptAuditPerformanceDto> DepartmentPerformance { get; set; } = new();
}

public class DeptAuditPerformanceDto
{
    public int DeptId { get; set; }
    public string DeptName { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public decimal AvgScorePercent { get; set; }
    public int OpenIssueCount { get; set; }
    public string Grade { get; set; } = "A";
}
