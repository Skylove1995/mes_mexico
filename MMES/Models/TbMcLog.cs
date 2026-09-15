using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbMcLog
{
    public int Id { get; set; }

    public int? ReqId { get; set; }

    public int? ItemId { get; set; }

    /// <summary>
    /// 0: Processing\n1: OK\n2: NG
    /// </summary>
    public string? CheckResult { get; set; }

    public string? Note { get; set; }

    public int? CheckBy { get; set; }

    public DateTime? CheckAt { get; set; }

    public virtual TbUser? CheckByNavigation { get; set; }

    public virtual TbMcChecksheet? Item { get; set; }

    public virtual TbMcRequest? Req { get; set; }

    public virtual ICollection<TbMcEvidence> TbMcEvidences { get; set; } = new List<TbMcEvidence>();
}
