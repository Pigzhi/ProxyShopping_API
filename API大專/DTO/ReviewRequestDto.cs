namespace API大專.DTO
{
    public class ReviewRequestDto
    {
        public string TargetType { get; set; } = null!; // product / commission
        public string? TargetCode { get; set; }
        public byte Result { get; set; } // 0=失敗, 1=通過
        public string? Reason { get; set; }
    }
}
