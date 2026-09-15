using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbSpecSqueegee
{
    public int Id { get; set; }

    public string? PcbPn { get; set; }

    public string? SqueegeeId { get; set; }

    public string? Size { get; set; }

    public string? Active { get; set; }

    public string? ModelName { get; set; }

    public string? Buyer { get; set; }

    public string? Board { get; set; }

    public DateTime? DateReceive { get; set; }

    public string? Remark { get; set; }

    public string? RackId { get; set; }
}
