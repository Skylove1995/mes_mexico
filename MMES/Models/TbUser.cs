using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbUser
{
    public int Id { get; set; }

    public string? UserName { get; set; }

    public int? RoleId { get; set; }

    public string? Position { get; set; }

    public string? Active { get; set; }

    public string? Email { get; set; }

    public string? FullName { get; set; }

    public string? PasswordHash { get; set; }

    public DateTime? CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public DateTime? LastLogin { get; set; }

    public virtual TbDept? Role { get; set; }

    public virtual ICollection<TbAuditResult> TbAuditResults { get; set; } = new List<TbAuditResult>();

    public virtual ICollection<TbDocApprovalHistory> TbDocApprovalHistories { get; set; } = new List<TbDocApprovalHistory>();

    public virtual ICollection<TbDocList> TbDocLists { get; set; } = new List<TbDocList>();

    public virtual ICollection<TbFaDefectDatum> TbFaDefectData { get; set; } = new List<TbFaDefectDatum>();

    public virtual ICollection<TbInternalDefectDatum> TbInternalDefectData { get; set; } = new List<TbInternalDefectDatum>();

    public virtual ICollection<TbMcLog> TbMcLogs { get; set; } = new List<TbMcLog>();

    public virtual ICollection<TbMcRequest> TbMcRequests { get; set; } = new List<TbMcRequest>();

    public virtual ICollection<TbRomHistory> TbRomHistories { get; set; } = new List<TbRomHistory>();
}
