using System;
using System.Collections.Generic;

namespace API大專.Models;

public partial class CommissionHistory
{
    public int HistoryId { get; set; }

    public int CommissionId { get; set; }

    public string Action { get; set; } = null!;

    public string? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? OldData { get; set; }

    public string? NewData { get; set; }

    public virtual Commission Commission { get; set; } = null!;
}
