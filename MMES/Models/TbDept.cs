using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbDept
{
    public int Id { get; set; }

    public string? Dept { get; set; }

    public string? Authority { get; set; }

    public int? IsActive { get; set; }

    public virtual ICollection<TbAuditChecksheet> TbAuditChecksheets { get; set; } = new List<TbAuditChecksheet>();

    public virtual ICollection<TbAuditResult> TbAuditResults { get; set; } = new List<TbAuditResult>();

    public virtual ICollection<TbDocList> TbDocLists { get; set; } = new List<TbDocList>();

    public virtual ICollection<TbUser> TbUsers { get; set; } = new List<TbUser>();
}
