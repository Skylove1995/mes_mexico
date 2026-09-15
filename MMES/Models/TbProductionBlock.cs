using System;
using System.Collections.Generic;

namespace MMES.Models;

public partial class TbProductionBlock
{
    /// <summary>
    /// Mã Barcode vỉ mạch
    /// </summary>
    public string Pid { get; set; } = null!;

    /// <summary>
    /// Mã đơn hàng W/O
    /// </summary>
    public string? WoName { get; set; }

    /// <summary>
    /// Mã máy phát hiện lỗi đầu tiên
    /// </summary>
    public string? McId { get; set; }

    /// <summary>
    /// Loại trạm phát hiện đầu tiên (SPI, MOI, AOI)
    /// </summary>
    public string? McType { get; set; }

    /// <summary>
    /// Ví dụ: Blocked at SPI issue USERPASS
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Mã người thao tác cho Pass (Operator ID)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 1 = Blocked (Chặn), 0 = Released (QA mở khóa)
    /// </summary>
    public sbyte Status { get; set; }

    /// <summary>
    /// Thời điểm Block lần đầu
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm QA Release
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
