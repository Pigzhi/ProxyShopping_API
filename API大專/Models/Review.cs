using System;
using System.Collections.Generic;

namespace API大專.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public string TargetType { get; set; } = null!;

    public int TargetId { get; set; }
    //public string? TargetCode { get; set; }

    public string ReviewerUid { get; set; } = null!;

    public byte Result { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }
}
