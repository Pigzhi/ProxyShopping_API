namespace API大專.DTO
{
    public class CommissionDetailDto
    {
        public int CommissionId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }

        public decimal Price { get; set; }
        public decimal Fee { get; set; }
        public decimal EscrowAmount { get; set; }

        public int Quantity { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }

        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime Deadline { get; set; }

        public string? ImageUrl { get; set; }
    }
}
