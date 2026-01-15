using System;
using System.Collections.Generic;

namespace API大專.Models;

public partial class ChatMessage
{
    public int MessageId { get; set; }

    public int? OrderId { get; set; }

    public string? SenderId { get; set; }

    public string? MessageText { get; set; }

    public DateTime? SentAt { get; set; }

    public virtual Order? Order { get; set; }

    public virtual User? Sender { get; set; }
}
