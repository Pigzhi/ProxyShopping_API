using System;
using System.Collections.Generic;

namespace API大專.Models;

public partial class Commission
{
    public int CommissionId { get; set; }

    public string? CreatorId { get; set; }

    public string? Title { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public string? Category { get; set; }

    public string? Location { get; set; }

    public DateTime? Deadline { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public decimal EscrowAmount { get; set; }

    public decimal Fee { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string ServiceCode { get; set; } = null!;

    public virtual ICollection<CommissionHistory> CommissionHistories { get; set; } = new List<CommissionHistory>();

    public virtual ICollection<CommissionOrder> CommissionOrders { get; set; } = new List<CommissionOrder>();

    public virtual ICollection<CommissionReceipt> CommissionReceipts { get; set; } = new List<CommissionReceipt>();

    public virtual ICollection<CommissionShipping> CommissionShippings { get; set; } = new List<CommissionShipping>();

    public virtual User? Creator { get; set; }
}
