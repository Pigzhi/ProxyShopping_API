using System;
using System.Collections.Generic;

namespace API大專.Models;

public partial class CommissionOrder
{
    public int OrderId { get; set; }

    public int CommissionId { get; set; }

    public string Status { get; set; } = null!;

    public decimal Amount { get; set; }

    public string BuyerId { get; set; } = null!;

    public string SellerId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public virtual Commission Commission { get; set; } = null!;
}
