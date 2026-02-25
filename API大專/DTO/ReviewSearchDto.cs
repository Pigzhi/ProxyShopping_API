namespace API大專.DTO
{
    public class ReviewSearchDto
    {
        public int ReviewId { get; set; }
        public string ReviewerUid { get; set; } = null!;
        public byte Result { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
