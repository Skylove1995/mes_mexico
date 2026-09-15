using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbProductionWorkOrder
{
    public int Id { get; set; }

    public string WoName { get; set; } = null!;

    public string? SmtAssyPn { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<TbProductionResult> TbProductionResults { get; set; } = new List<TbProductionResult>();
}
