using System;
using System.Collections.Generic;

namespace API大專.Models;

public partial class CommissionShipping
{
    public int ShippingId { get; set; }

    public int CommissionId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ShippedAt { get; set; }

    public string ShippedBy { get; set; } = null!;

    public string? LogisticsName { get; set; }

    public string? TrackingNumber { get; set; }

    public string? Remark { get; set; }
}
