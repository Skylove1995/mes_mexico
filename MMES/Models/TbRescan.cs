using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbRescan
{
    public int Id { get; set; }

    public string? Pba { get; set; }

    public string? ModelName { get; set; }

    public string? Pid { get; set; }

    public string? PartNo { get; set; }

    public string? WorkOrder { get; set; }

    public DateTime? ScanAt { get; set; }

    public DateTime? RescanAt { get; set; }

    public int? Qty { get; set; }

    public string? ModelSuffix { get; set; }
}
