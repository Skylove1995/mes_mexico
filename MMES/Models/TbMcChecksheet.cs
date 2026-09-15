using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbMcChecksheet
{
    public int Id { get; set; }

    public string? Process { get; set; }

    public string? CheckItem { get; set; }

    public string? Spec { get; set; }

    /// <summary>
    /// DYNAMIC / FIX
    /// </summary>
    public string? SpecType { get; set; }

    public string? EvidenceReq { get; set; }

    /// <summary>
    /// Y/N
    /// </summary>
    public string? Active { get; set; }

    public string? Pic { get; set; }

    public string? McFor { get; set; }

    public virtual ICollection<TbMcLog> TbMcLogs { get; set; } = new List<TbMcLog>();
}
