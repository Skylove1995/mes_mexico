using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbMcEvidence
{
    public int Id { get; set; }

    public int? LogId { get; set; }

    public string? Url { get; set; }

    public string? MimeType { get; set; }

    public virtual TbMcLog? Log { get; set; }
}
