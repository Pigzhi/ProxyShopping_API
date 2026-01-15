using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API大專.Models;

public partial class Commission
{
    [Key]
    [Column("commission_id")]
    public int CommissionId { get; set; }
    [Column("service_code")]
    public string ServiceCode { get; set; } = null!;

    [Column("creator_id")]
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

    [Column("escrowAmount")]
    public decimal EscrowAmount { get; set; }

    public decimal Fee { get; set; }

    public int? FailCount { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CommissionHistory> CommissionHistories { get; set; } = new List<CommissionHistory>();

    public virtual ICollection<CommissionOrder> CommissionOrders { get; set; } = new List<CommissionOrder>();

    public virtual User? Creator { get; set; }
}
