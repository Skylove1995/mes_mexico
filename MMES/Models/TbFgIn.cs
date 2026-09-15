using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbFgIn
{
    public int Id { get; set; }

    public string? Locator { get; set; }

    public string? Pba { get; set; }

    public string? PartNo { get; set; }

    public string? WorkOrder { get; set; }

    public string? ModelName { get; set; }

    public string? ModelSuffix { get; set; }

    public int? Qty { get; set; }

    public DateTime? ScanTime { get; set; }

    public DateOnly? ScanDate { get; set; }
}
