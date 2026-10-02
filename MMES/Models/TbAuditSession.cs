using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMES.Models;

[Table("tb_audit_session")]
public partial class TbAuditSession
{
    public int Id { get; set; }

    [Column("AUDIT_TYPE_ID")]
    public int? AuditTypeId { get; set; }

    [Column("DEPT_ID")]
    public int? DeptId { get; set; }

    [Column("AUDIT_MONTH")]
    public DateTime? AuditMonth { get; set; }

    [Column("SHIFT")]
    public string? Shift { get; set; }

    [Column("AUDITOR_ID")]
    public int? AuditorId { get; set; }

    [Column("CHECKED_BY_ID")]
    public int? CheckedById { get; set; }

    [Column("CHECKED_AT")]
    public DateTime? CheckedAt { get; set; }

    [Column("APPROVED_BY_ID")]
    public int? ApprovedById { get; set; }

    [Column("APPROVED_AT")]
    public DateTime? ApprovedAt { get; set; }

    [Column("STATUS_ID")]
    public int? StatusId { get; set; }

    [Column("TOTAL_SCORE")]
    public decimal? TotalScore { get; set; }

    [Column("TOTAL_SCORE_TARGET")]
    public decimal? TotalScoreTarget { get; set; }

    [Column("SCORE_PERCENT")]
    public decimal? ScorePercent { get; set; }

    [Column("GRADE")]
    public string? Grade { get; set; }

    [Column("NOTE")]
    public string? Note { get; set; }

    [Column("CREATED_AT")]
    public DateTime? CreatedAt { get; set; }

    [Column("UPDATED_AT")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("AuditTypeId")]
    public virtual TbAuditType? AuditType { get; set; }

    [ForeignKey("DeptId")]
    public virtual TbDept? Dept { get; set; }

    [ForeignKey("AuditorId")]
    public virtual TbUser? Auditor { get; set; }

    [ForeignKey("CheckedById")]
    public virtual TbUser? CheckedBy { get; set; }

    [ForeignKey("ApprovedById")]
    public virtual TbUser? ApprovedBy { get; set; }

    [ForeignKey("StatusId")]
    public virtual TbAuditStatus? Status { get; set; }

    public virtual ICollection<TbAuditResult> TbAuditResults { get; set; } = new List<TbAuditResult>();
}
