namespace API大專.DTO
{
    public class UploadReceiptDto
    {
        public IFormFile Image { get; set; }          
        public decimal? ReceiptAmount { get; set; }   
        public DateTime? ReceiptDate { get; set; }    
        public string? Remark { get; set; }        
    }
}
