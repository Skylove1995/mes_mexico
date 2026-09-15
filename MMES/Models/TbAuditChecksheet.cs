using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbAuditChecksheet
{
    public int Id { get; set; }

    public int? AuditTypeId { get; set; }

    public string? Category { get; set; }

    public string? AuditItem { get; set; }

    public string? Criteria { get; set; }

    public int? MaxScore { get; set; }

    public string? Critical { get; set; }

    public string? Active { get; set; }

    public int? ForDept { get; set; }

    public virtual TbAuditType? AuditType { get; set; }

    public virtual TbDept? ForDeptNavigation { get; set; }

    public virtual ICollection<TbAuditResult> TbAuditResults { get; set; } = new List<TbAuditResult>();
}
