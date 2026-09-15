using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbImportCursor
{
    public string LineKey { get; set; } = null!;

    public DateTime LastStampUtc { get; set; }

    public string LastFileName { get; set; } = null!;
}
