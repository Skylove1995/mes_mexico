using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMES.Models;

[Table("tb_audit_result")]
public partial class TbAuditResult
{
    public int Id { get; set; }

    [Column("AUDIT_ITEM_ID")]
    public int? AuditItemId { get; set; }

    [Column("SESSION_ID")]
    public int SessionId { get; set; }

    [Column("SCORE")]
    public decimal? Score { get; set; }

    [Column("JUDGE")]
    public string? Judge { get; set; }

    [Column("NOTE")]
    public string? Note { get; set; }

    [Column("ACTION_CONTENT")]
    public string? ActionContent { get; set; }

    [Column("AUDIT_BY_ID")]
    public int? AuditById { get; set; }

    [Column("AUDIT_DATE")]
    public DateTime? AuditDate { get; set; }

    [Column("ISSUE_OWNER_ID")]
    public int? IssueOwnerId { get; set; }

    [Column("STATUS_ID")]
    public int? StatusId { get; set; }

    [Column("MODEL_ID")]
    public int? ModelId { get; set; }

    [Column("LINE_ID")]
    public int? LineId { get; set; }

    [Column("LAST_COMMENT")]
    public string? LastComment { get; set; }

    [Column("ACTION_DATE")]
    public DateTime? ActionDate { get; set; }

    [ForeignKey("AuditById")]
    public virtual TbUser? AuditBy { get; set; }

    [ForeignKey("AuditItemId")]
    public virtual TbAuditChecksheet? AuditItem { get; set; }

    [ForeignKey("SessionId")]
    public virtual TbAuditSession? Session { get; set; }

    [ForeignKey("IssueOwnerId")]
    public virtual TbDept? IssueOwner { get; set; }

    [ForeignKey("LineId")]
    public virtual TbLine? Line { get; set; }

    [ForeignKey("ModelId")]
    public virtual TbModel? Model { get; set; }

    [ForeignKey("StatusId")]
    public virtual TbAuditStatus? Status { get; set; }

    public virtual ICollection<TbAuditAction> TbAuditActions { get; set; } = new List<TbAuditAction>();

    public virtual ICollection<TbAuditActionHi> TbAuditActionHis { get; set; } = new List<TbAuditActionHi>();

    public virtual ICollection<TbAuditEvidenceHi> TbAuditEvidenceHis { get; set; } = new List<TbAuditEvidenceHi>();
}
