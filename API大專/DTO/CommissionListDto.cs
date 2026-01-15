namespace API大專.DTO
{
    public class CommissionListDto
    {
        public int CommissionId { get; set; }
        public string? Title { get; set; }
        public decimal Price { get; set; }
        public string? Status { get; set; }
        public string? Category { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime Deadline { get; set; }
    }
}
