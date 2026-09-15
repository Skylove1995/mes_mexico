using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbSpecStencil
{
    public int Id { get; set; }

    public string? RackId { get; set; }

    public string? RackNo { get; set; }

    public string? StencilId { get; set; }

    public string? ManagerNo { get; set; }

    public string? StencilPn { get; set; }

    public string? Buyer { get; set; }

    public string? Model { get; set; }

    public string? Division { get; set; }

    public string? Side { get; set; }

    public string? Ver { get; set; }

    public string? StencilThickness { get; set; }

    public int? Qty { get; set; }

    public string? StencilType { get; set; }

    public string? Remark { get; set; }

    public DateTime? DateReceive { get; set; }

    public string? Active { get; set; }
}
