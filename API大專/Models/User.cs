using System;
using System.Collections.Generic;

namespace API大專.Models;

public partial class User
{
    public string Uid { get; set; } = null!;
    public string identity { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Phone { get; set; }
    public string? address { get; set; }

    public decimal? Balance { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? DisabledUntil { get; set; }

    public virtual ICollection<BalanceLog> BalanceLogs { get; set; } = new List<BalanceLog>();

    public virtual ICollection<Commission> Commissions { get; set; } = new List<Commission>();

    public virtual ICollection<DepositOrder> DepositOrders { get; set; } = new List<DepositOrder>();
}
