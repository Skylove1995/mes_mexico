using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbDocApprovalHistory
{
    public int Id { get; set; }

    public int? DocId { get; set; }

    public int? LineNo { get; set; }

    public int? ApproveBy { get; set; }

    public string? Comment { get; set; }

    public DateTime? ApproveTime { get; set; }

    /// <summary>
    /// 0: draft\n1: approved\n2: reject
    /// </summary>
    public int? Status { get; set; }

    public virtual TbUser? ApproveByNavigation { get; set; }

    public virtual TbDocList? Doc { get; set; }
}
