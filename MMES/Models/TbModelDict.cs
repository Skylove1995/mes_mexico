using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbModelDict
{
    public int Id { get; set; }

    public string? PartNo { get; set; }

    public string? Board { get; set; }

    public string? ModelName { get; set; }

    public string? ModelSuffix { get; set; }
}
