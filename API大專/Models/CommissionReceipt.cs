using System;
using System.Collections.Generic;

namespace API大專.Models;

public partial class CommissionReceipt
{
    public int ReceiptId { get; set; }

    public int CommissionId { get; set; }

    public string UploadedBy { get; set; } = null!;

    public string ReceiptImageUrl { get; set; } = null!;

    public decimal? ReceiptAmount { get; set; }

    public DateTime? ReceiptDate { get; set; }

    public DateTime UploadedAt { get; set; }

    public string? Remark { get; set; }
}
