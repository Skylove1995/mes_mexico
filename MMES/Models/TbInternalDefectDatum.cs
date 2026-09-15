using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbInternalDefectDatum
{
    public int Id { get; set; }

    public string? Pid { get; set; }

    public DateOnly? DefectDate { get; set; }

    public int? RepairMan { get; set; }

    public string? Process { get; set; }

    public string? DefectName { get; set; }

    public string? Location { get; set; }

    public string? Side { get; set; }

    public string? Cause { get; set; }

    public string? DefectFrom { get; set; }

    public string? ActionType { get; set; }

    public string? ActionContent { get; set; }

    public DateOnly? ActionDate { get; set; }

    public string? WorkOrder { get; set; }

    public string? PartNo { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime? UploadTime { get; set; }

    public virtual TbUser? RepairManNavigation { get; set; }
}
