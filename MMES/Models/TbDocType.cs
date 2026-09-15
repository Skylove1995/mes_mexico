using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbDocType
{
    public int Id { get; set; }

    public string? DocType { get; set; }

    public virtual ICollection<TbDocList> TbDocLists { get; set; } = new List<TbDocList>();
}
