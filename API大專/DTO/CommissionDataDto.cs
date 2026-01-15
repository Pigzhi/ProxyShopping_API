using System.ComponentModel.DataAnnotations;

namespace API大專.DTO
{
    public class CommissionDataDto
    {
        public string? Title { get; set; }

        public string ServiceCode { get; set; } = null;
        public string? Description { get; set; }
        public Decimal TotalPrice { get; set; }
        public int? Quantity { get; set; }

        public string? Category { get; set; }
        public DateTime Deadline { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Location { get; set; }
        public string? ImageUrl { get; set; }
    }
}
