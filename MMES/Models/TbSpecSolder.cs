using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbSpecSolder
{
    public int Id { get; set; }

    public string? Buyer { get; set; }

    public string? ModelSuffix { get; set; }

    public string? ModelName { get; set; }

    public string? Board { get; set; }

    public string? PcbPn { get; set; }

    public string? SmtPn { get; set; }

    public string? PcbaPn { get; set; }

    public string? SolderTypeBot { get; set; }

    public string? SolderTypeTop { get; set; }

    public string? Active { get; set; }
}
