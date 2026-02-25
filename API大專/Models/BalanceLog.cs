using System;
using System.Collections.Generic;

namespace API大專.Models;

public partial class BalanceLog
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string Action { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal? BeforeBalance { get; set; }

    public decimal? AfterBalance { get; set; }

    public string? RefType { get; set; }

    public int? RefId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
