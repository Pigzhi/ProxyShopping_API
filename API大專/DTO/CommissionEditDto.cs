using API大專.Validation;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace API大專.DTO
{
    public class CommissionEditDto
    {
        [Required(ErrorMessage ="標題必填")]
        [StringLength(50, ErrorMessage ="標題長度不可超過50字")]
        public string Title { get; set; } = null!;


        [Required(ErrorMessage = "描述必填")]
        [StringLength(500, ErrorMessage = "描述長度不可超過 500 字")]
        public string Description { get; set; } = null!;


        [Required(ErrorMessage = "價格必填")]
        [Range(1, 999999, ErrorMessage = "價格必須大於 0")]
        public decimal Price { get; set; }


        [Required(ErrorMessage = "數量必填")]
        [Range(1, 1000, ErrorMessage = "數量必須介於 1 ~ 1000")]
        public int Quantity { get; set; }


        [Required(ErrorMessage = "分類必填")]
        public string Category { get; set; } = null!;



        [Required(ErrorMessage = "截止時間必填")]
        [FutureDate(ErrorMessage = "截止時間必須大於今天")]
        public DateTime Deadline { get; set; }

        public string? Location { get; set; }
        public IFormFile? Image { get; set; }
    }
}
