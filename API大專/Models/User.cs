using System;
using System.Collections.Generic;

namespace API大專.Models;

public partial class User
{
    public string Uid { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Phone { get; set; }

    public decimal? Balance { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Commission> Commissions { get; set; } = new List<Commission>();
}
