using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbDocList
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public string? Title { get; set; }

    public string? Version { get; set; }

    public string? Url { get; set; }

    public int? DocType { get; set; }

    public string? Note { get; set; }

    public int? UploadBy { get; set; }

    public DateTime? UploadAt { get; set; }

    public int? Owner { get; set; }

    public virtual TbDocType? DocTypeNavigation { get; set; }

    public virtual TbDept? OwnerNavigation { get; set; }

    public virtual ICollection<TbDocApprovalHistory> TbDocApprovalHistories { get; set; } = new List<TbDocApprovalHistory>();

    public virtual TbUser? UploadByNavigation { get; set; }
}
