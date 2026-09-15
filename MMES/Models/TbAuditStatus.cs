using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbAuditStatus
{
    public int Id { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<TbAuditResult> TbAuditResults { get; set; } = new List<TbAuditResult>();
}
