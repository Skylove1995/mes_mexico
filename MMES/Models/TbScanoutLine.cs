using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbScanoutLine
{
    public int Id { get; set; }

    public string? Ip { get; set; }

    public string? Model { get; set; }

    public string? LineNo { get; set; }
}
