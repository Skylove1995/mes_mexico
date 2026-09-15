using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbSpecBlockJig
{
    public int Id { get; set; }

    public string? Buyer { get; set; }

    public string? RackNo { get; set; }

    public string? BlockJigId { get; set; }

    public string? Pn { get; set; }

    public string? BlockJigName { get; set; }

    public string? ModelName { get; set; }

    public string? Board { get; set; }

    public string? Ver { get; set; }

    public string? Side { get; set; }

    public int? Qty { get; set; }

    public string? Active { get; set; }

    public string? Remark { get; set; }

    public DateTime? DateReceive { get; set; }

    public string? Status { get; set; }
}
