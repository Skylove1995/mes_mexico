using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbProductionFullView
{
    public long Id { get; set; }

    public string Pid { get; set; } = null!;

    public int? Aray { get; set; }

    public int? Seq { get; set; }

    public string? McType { get; set; }

    public string McId { get; set; } = null!;

    public string? TestResult { get; set; }

    public string? UserResult { get; set; }

    public string WOName { get; set; } = null!;

    public string? SmtAssyPN { get; set; }

    public DateTime TransactionDate { get; set; }

    public DateTime? CreatedAt { get; set; }
}
