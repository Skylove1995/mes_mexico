using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMES.Models;

[Table("tb_smt_line_target")]
public partial class TbSmtLineTarget
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("prod_date")]
    public DateOnly ProdDate { get; set; }

    [Column("line_code")]
    [StringLength(50)]
    public string LineCode { get; set; } = null!;

    [Column("target_qty")]
    public int TargetQty { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }
}
