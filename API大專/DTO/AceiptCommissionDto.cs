namespace API大專.DTO
{
    public class AceiptCommissionManageDto
    {
        public string ServiceCode { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? ImageUrl { get; set; }
        public decimal PlatformFee { get; set; }


        public bool CanUpdateReceipt { get; set; }
        public bool CanUpdateShip { get; set; }
        public bool CanViewReceipt { get; set; }
        public bool CanViewShipping { get; set; }
    }
}
