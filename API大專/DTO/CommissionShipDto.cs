namespace API大專.DTO
{
    public class CommissionShipDto
    {
        public string LogisticsName { get; set; } = null!;//去寄的物流名稱
        public string? TrackingNumber { get; set; }//單號
        public string? Remark { get; set; }//說明
    }
}
