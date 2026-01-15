using System;
using System.Collections.Generic;

namespace API大專.Models;


public partial class Order
{
    public int OrderId { get; set; }

    public int? CommissionId { get; set; }

    public string? BuyerId { get; set; }

    public string? SellerId { get; set; }

    public string? OrderStatus { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal? SystemFee { get; set; }

    public string? ShippingAddress { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public decimal EscrowAmount { get; set; }

    public virtual User? Buyer { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual Commission? Commission { get; set; }
    public virtual User? Seller { get; set; }
}
