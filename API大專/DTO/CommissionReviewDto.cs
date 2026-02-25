namespace API大專.DTO
{
    public class CommissionReviewDto
    {
        public string? ServiceCode { get; set; }
        public string? Title { get; set; }
        public string? CreatorId { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? Deadline { get; set; }
        public decimal EscrowAmount { get; set; }
        public string? LatestFailReason { get; set; }
    }

}
