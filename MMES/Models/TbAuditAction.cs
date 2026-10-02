using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMES.Models;

[Table("tb_audit_action")]
public partial class TbAuditAction
{
    public int Id { get; set; }

    [Column("AUDIT_RESULT_ID")]
    public int AuditResultId { get; set; }

    [Column("ACTION_TYPE")]
    public string? ActionType { get; set; }

    [Column("DESCRIPTION")]
    public string? Description { get; set; }

    [Column("PIC_ID")]
    public int? PicId { get; set; }

    [Column("DUE_DATE")]
    public DateTime? DueDate { get; set; }

    [Column("STATUS_ID")]
    public int? StatusId { get; set; }

    [Column("COMPLETED_AT")]
    public DateTime? CompletedAt { get; set; }

    [Column("CREATED_BY_ID")]
    public int? CreatedById { get; set; }

    [Column("CREATED_AT")]
    public DateTime? CreatedAt { get; set; }

    [Column("UPDATED_AT")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("AuditResultId")]
    public virtual TbAuditResult? AuditResult { get; set; }

    [ForeignKey("PicId")]
    public virtual TbUser? Pic { get; set; }

    [ForeignKey("StatusId")]
    public virtual TbAuditStatus? Status { get; set; }

    [ForeignKey("CreatedById")]
    public virtual TbUser? CreatedBy { get; set; }

    public virtual ICollection<TbAuditActionEvidence> TbAuditActionEvidences { get; set; } = new List<TbAuditActionEvidence>();
}
