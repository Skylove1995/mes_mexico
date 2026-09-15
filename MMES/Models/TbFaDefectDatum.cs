using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbFaDefectDatum
{
    public int Id { get; set; }

    public string? Pid { get; set; }

    public string? NcrNo { get; set; }

    public DateOnly? NcrDate { get; set; }

    public string? NcrStatus { get; set; }

    public DateOnly? NcrCloseDate { get; set; }

    public string? NcrUrl { get; set; }

    public int? Pic { get; set; }

    public string? DefectName { get; set; }

    public string? Location { get; set; }

    /// <summary>
    /// ISSUE OWNER
    /// </summary>
    public string? DefectFrom { get; set; }

    public string? WorkOrder { get; set; }

    public string? PartNo { get; set; }

    public string? ModelName { get; set; }

    public string? DefectImageUrl { get; set; }

    public string? Category1 { get; set; }

    public string? Category2 { get; set; }

    public string? AoiHistory { get; set; }

    public string? FctHistory { get; set; }

    public string? FctRetest { get; set; }

    public string? FctNote { get; set; }

    public string? Cause { get; set; }

    public string? ActionType { get; set; }

    public DateOnly? ActionDate { get; set; }

    public string? SampleLocation { get; set; }

    public string? HseReportUrl { get; set; }

    public string? Remark { get; set; }

    public virtual TbUser? PicNavigation { get; set; }
}
