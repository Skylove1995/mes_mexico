using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbMcRequest
{
    public int Id { get; set; }

    public DateTime? ReqAt { get; set; }

    public DateTime? DoneAt { get; set; }

    public int? WoBeforeId { get; set; }

    public int? WoAfterId { get; set; }

    /// <summary>
    /// 0: Prrocessing\\n1: Done\\n2: Cancel
    /// </summary>
    public string? Status { get; set; }

    public int? ReqBy { get; set; }

    public int? LineId { get; set; }

    public string? WorkFace { get; set; }

    public string? McFor { get; set; }

    public virtual TbLine? Line { get; set; }

    public virtual TbUser? ReqByNavigation { get; set; }

    public virtual ICollection<TbMcLog> TbMcLogs { get; set; } = new List<TbMcLog>();

    public virtual TbProductionPlan? WoAfter { get; set; }

    public virtual TbProductionPlan? WoBefore { get; set; }
}
