using System;
using System.Collections.Generic;

namespace API大專.Models;

public partial class DepositOrder
{
    public int DepositOrderId { get; set; }

    public string OrderNo { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual User User { get; set; } = null!;
}
