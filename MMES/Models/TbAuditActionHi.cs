using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbAuditActionHi
{
    public int Id { get; set; }

    public string? Url { get; set; }

    public int? AuditResultId { get; set; }

    public string? MimeType { get; set; }

    public virtual TbAuditResult? AuditResult { get; set; }
}
