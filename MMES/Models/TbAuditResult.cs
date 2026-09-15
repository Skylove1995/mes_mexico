using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbAuditResult
{
    public int Id { get; set; }

    public int? AuditItemId { get; set; }

    public int? Score { get; set; }

    public string? Judge { get; set; }

    public string? Note { get; set; }

    public string? ActionContent { get; set; }

    public int? AuditById { get; set; }

    public DateTime? AuditDate { get; set; }

    public int? IssueOwnerId { get; set; }

    public int? StatusId { get; set; }

    public int? ModelId { get; set; }

    public int? LineId { get; set; }

    public string? LastComment { get; set; }

    public DateTime? ActionDate { get; set; }

    public virtual TbUser? AuditBy { get; set; }

    public virtual TbAuditChecksheet? AuditItem { get; set; }

    public virtual TbDept? IssueOwner { get; set; }

    public virtual TbLine? Line { get; set; }

    public virtual TbModel? Model { get; set; }

    public virtual TbAuditStatus? Status { get; set; }

    public virtual ICollection<TbAuditActionHi> TbAuditActionHis { get; set; } = new List<TbAuditActionHi>();

    public virtual ICollection<TbAuditEvidenceHi> TbAuditEvidenceHis { get; set; } = new List<TbAuditEvidenceHi>();
}
