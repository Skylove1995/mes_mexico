using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMES.Models;

[Table("tb_pickup_miss")]
public partial class TbPickupMiss
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("prod_date")]
    public DateOnly ProdDate { get; set; }

    [Column("line_code")]
    [StringLength(50)]
    public string LineCode { get; set; } = null!;

    [Column("machine_name")]
    [StringLength(50)]
    public string MachineName { get; set; } = null!;

    [Column("time_slot")]
    [StringLength(20)]
    public string TimeSlot { get; set; } = null!;

    [Column("pickup_rate", TypeName = "decimal(5, 2)")]
    public decimal PickupRate { get; set; }

    [Column("pickup_loss_rate", TypeName = "decimal(5, 2)")]
    public decimal PickupLossRate { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }
}
