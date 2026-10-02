using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMES.Models;

[Table("tb_audit_action_evidence")]
public partial class TbAuditActionEvidence
{
    public int Id { get; set; }

    [Column("ACTION_ID")]
    public int ActionId { get; set; }

    [Column("URL")]
    public string Url { get; set; } = string.Empty;

    [Column("MIME_TYPE")]
    public string? MimeType { get; set; }

    [Column("CREATED_AT")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("ActionId")]
    public virtual TbAuditAction? Action { get; set; }
}
