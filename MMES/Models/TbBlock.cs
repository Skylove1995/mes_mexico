using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbBlock
{
    public string Pid { get; set; } = null!;

    public string? History { get; set; }

    /// <summary>
    /// B: Block - R: Release
    /// </summary>
    public string? Status { get; set; }

    public DateTime? BlockAt { get; set; }

    public DateTime? ReleaseAt { get; set; }

    public string? Pba { get; set; }

    public string? PartNo { get; set; }

    public string? WorkOrder { get; set; }
}
