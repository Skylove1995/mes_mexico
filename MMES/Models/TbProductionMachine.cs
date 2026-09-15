using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbProductionMachine
{
    public int Id { get; set; }

    public string McId { get; set; } = null!;

    public string? McType { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<TbProductionResult> TbProductionResults { get; set; } = new List<TbProductionResult>();
}
