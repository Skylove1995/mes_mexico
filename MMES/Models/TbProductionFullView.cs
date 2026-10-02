using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MMES.Models;

[Keyless]
[Table("tb_production_full_view")]
public partial class TbProductionFullView
{
    [Column("id")]
    public long? Id { get; set; }

    [Column("pid")]
    public string? Pid { get; set; }

    [Column("Aray")]
    public int? Aray { get; set; }

    [Column("Seq")]
    public int? Seq { get; set; }

    [Column("MC Type")]
    public string? McType { get; set; }

    [Column("MC ID")]
    public string? McId { get; set; }

    [Column("Test Result")]
    public string? TestResult { get; set; }

    [Column("User Result")]
    public string? UserResult { get; set; }

    [Column("W/O Name")]
    public string? WOName { get; set; }

    [Column("SMT Assy P/N")]
    public string? SmtAssyPN { get; set; }

    [Column("Transaction Date")]
    public DateTime? TransactionDate { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
