using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbModel
{
    public int Id { get; set; }

    public string? ModelName { get; set; }

    public virtual ICollection<TbAuditResult> TbAuditResults { get; set; } = new List<TbAuditResult>();
}
