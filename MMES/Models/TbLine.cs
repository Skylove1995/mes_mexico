using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbLine
{
    public int Id { get; set; }

    public string? LineName { get; set; }

    public virtual ICollection<TbAuditResult> TbAuditResults { get; set; } = new List<TbAuditResult>();

    public virtual ICollection<TbMcRequest> TbMcRequests { get; set; } = new List<TbMcRequest>();
}
