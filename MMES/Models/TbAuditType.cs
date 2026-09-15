using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbAuditType
{
    public int Id { get; set; }

    public string? AuditType { get; set; }

    public virtual ICollection<TbAuditChecksheet> TbAuditChecksheets { get; set; } = new List<TbAuditChecksheet>();
}
