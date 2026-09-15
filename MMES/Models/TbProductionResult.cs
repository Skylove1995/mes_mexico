using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbProductionResult
{
    public long Id { get; set; }

    public string Pid { get; set; } = null!;

    public int? ArraySeq { get; set; }

    public int? Seq { get; set; }

    public int WoId { get; set; }

    public int McId { get; set; }

    public string? TestResult { get; set; }

    public string? UserResult { get; set; }

    public DateTime TransactionDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual TbProductionMachine Mc { get; set; } = null!;

    public virtual TbProductionWorkOrder Wo { get; set; } = null!;
}
