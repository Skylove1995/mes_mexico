using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbDeptGroup
{
    public int Id { get; set; }

    public string? ParrentDept { get; set; }

    public string? ChildDept { get; set; }
}
