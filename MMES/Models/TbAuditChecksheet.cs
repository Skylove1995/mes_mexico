using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMES.Models;

[Table("tb_audit_checksheet")]
public partial class TbAuditChecksheet
{
    public int Id { get; set; }

    [Column("AUDIT_TYPE_ID")]
    public int? AuditTypeId { get; set; }

    [Column("CATEGORY")]
    public string? Category { get; set; }

    [Column("SEQ_NO")]
    public int? SeqNo { get; set; }

    [Column("AUDIT_ITEM")]
    public string? AuditItem { get; set; }

    [Column("AUDIT_ITEM_TRANSLATE")]
    public string? AuditItemTranslate { get; set; }

    [Column("CRITERIA")]
    public string? Criteria { get; set; }

    [Column("CRITICAL")]
    public string? Critical { get; set; }

    [Column("MAX_SCORE")]
    public decimal? MaxScore { get; set; }

    [Column("TIME_LIMIT")]
    public string? TimeLimit { get; set; }

    [Column("ACTIVE")]
    public string? Active { get; set; }

    [Column("FOR_DEPT")]
    public int? ForDept { get; set; }

    [ForeignKey("AuditTypeId")]
    public virtual TbAuditType? AuditType { get; set; }

    [ForeignKey("ForDept")]
    public virtual TbDept? ForDeptNavigation { get; set; }

    public virtual ICollection<TbAuditResult> TbAuditResults { get; set; } = new List<TbAuditResult>();
}
